using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ICouponService _coupons;

        public OrderController(AppDbContext db, IConfiguration config, ICouponService coupons)
        {
            _db = db;
            _config = config;
            _coupons = coupons;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /api/order/create   { couponCode? }
        // Prices the cart, applies the coupon, saves a PENDING order, and asks Razorpay for that amount.
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto? dto)
        {
            var userId = GetCurrentUserId();

            var cartItems = await _db.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest(new { message = "Your cart is empty." });

            foreach (var item in cartItems)
                if (item.Product!.Quantity < item.Quantity)
                    return BadRequest(new { message = $"Only {item.Product.Quantity} of {item.Product.Name} left in stock." });

            var subTotal = cartItems.Sum(c => c.Product!.Price * c.Quantity);
            var result = await _coupons.EvaluateAsync(dto?.CouponCode, userId, subTotal);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            var total = result.Total;
            var amountInPaise = (int)Math.Round(total * 100m);     // Razorpay wants paise, rounded not truncated

            var keyId = _config["Razorpay:KeyId"]!;
            var keySecret = _config["Razorpay:KeySecret"]!;

            var client = new RazorpayClient(keyId, keySecret);
            var options = new Dictionary<string, object>
            {
                { "amount", amountInPaise },
                { "currency", "INR" },
                { "receipt", $"receipt_{userId}_{DateTime.UtcNow.Ticks}" }
            };
            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);
            var razorpayOrderId = razorpayOrder["id"].ToString();

            // Save OUR order now, items copied with their current prices
            var order = new EcommerceApi.Models.Order
            {
                UserId = userId,
                Status = "Pending",
                SubTotal = subTotal,
                DiscountAmount = result.Discount,
                TotalAmount = total,
                CouponCode = result.Coupon?.Code,
                CouponId = result.Coupon?.Id,
                RazorpayOrderId = razorpayOrderId
            };
            foreach (var item in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product!.Name,
                    Price = item.Product.Price,
                    Quantity = item.Quantity
                });
            }
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return Ok(new CreateOrderResponseDto
            {
                RazorpayOrderId = razorpayOrderId!,
                KeyId = keyId,
                AmountInPaise = amountInPaise,
                SubTotal = subTotal,
                Discount = result.Discount,
                Total = total
            });
        }

        // POST /api/order/verify
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment(VerifyPaymentDto dto)
        {
            var userId = GetCurrentUserId();

            // The order must exist, and belong to THIS user
            var order = await _db.Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.RazorpayOrderId == dto.RazorpayOrderId && o.UserId == userId);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (order.Status == "Paid")        // a repeated call must not charge or reduce anything twice
                return Ok(new { message = "Order already confirmed.", orderId = order.Id });

            // Creating the client also gives the library the secret it uses for the check below
            _ = new RazorpayClient(_config["Razorpay:KeyId"]!, _config["Razorpay:KeySecret"]!);
            var attributes = new Dictionary<string, string>
            {
                { "razorpay_order_id", dto.RazorpayOrderId },
                { "razorpay_payment_id", dto.RazorpayPaymentId },
                { "razorpay_signature", dto.RazorpaySignature }
            };
            try { Utils.verifyPaymentSignature(attributes); }
            catch (Exception) { return BadRequest(new { message = "Payment verification failed." }); }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. stock: one atomic UPDATE per item, so two buyers can never take the same last unit
                foreach (var item in order.Items)
                {
                    var productId = item.ProductId;
                    var qty = item.Quantity;
                    var rows = await _db.Products
                        .Where(p => p.Id == productId && p.Quantity >= qty)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity - qty));
                    if (rows == 0)
                        throw new InvalidOperationException($"Not enough stock for {item.ProductName}.");
                }

                // 2. coupon: atomic usage-limit check and increment, then the redemption record
                if (order.CouponId.HasValue)
                {
                    var couponId = order.CouponId.Value;
                    var rows = await _db.Coupons
                        .Where(c => c.Id == couponId && c.IsActive && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));
                    if (rows == 0)
                        throw new InvalidOperationException("This coupon is no longer available.");

                    _db.CouponRedemptions.Add(new CouponRedemption
                    {
                        CouponId = couponId,
                        UserId = userId,
                        OrderId = order.Id,
                        DiscountAmount = order.DiscountAmount
                    });
                }

                // 3. mark the order paid, and remove only the purchased items from the cart
                order.Status = "Paid";
                order.RazorpayPaymentId = dto.RazorpayPaymentId;

                var paidProductIds = order.Items.Select(i => i.ProductId).ToList();
                var cartRows = await _db.CartItems
                    .Where(c => c.UserId == userId && paidProductIds.Contains(c.ProductId))
                    .ToListAsync();
                _db.CartItems.RemoveRange(cartRows);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }

            // if you added Kafka, keep your publish line here, after the commit:
            // _kafkaProducer.PublishOrderPlaced(order.Id, userId);

            return Ok(new { message = "Payment verified, order placed.", orderId = order.Id });
        }

        // GET /api/order/my-orders   (paid orders only)
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetCurrentUserId();

            var orders = await _db.Orders
                .Where(o => o.UserId == userId && o.Status == "Paid")
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    SubTotal = o.SubTotal > 0 ? o.SubTotal : o.TotalAmount,   // orders from before coupons existed
                    DiscountAmount = o.DiscountAmount,
                    CouponCode = o.CouponCode,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    Items = o.Items.Select(i => new OrderItemResponseDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}