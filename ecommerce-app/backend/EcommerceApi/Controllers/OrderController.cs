using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
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

        public OrderController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // POST /api/order/create
        // Step 1 of checkout: calculate the cart total, ask Razorpay to create
        // an "order" on their side, and hand back what the frontend needs
        // to actually open the payment popup.
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder()
        {
            var userId = GetCurrentUserId();

            var cartItems = await _db.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest(new { message = "Your cart is empty." });

            decimal total = cartItems.Sum(c => c.Product!.Price * c.Quantity);
            int amountInPaise = (int)(total * 100); // Razorpay expects the smallest currency unit

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

            return Ok(new CreateOrderResponseDto
            {
                RazorpayOrderId = razorpayOrder["id"].ToString(),
                KeyId = keyId,
                AmountInPaise = amountInPaise
            });
        }

        // POST /api/order/verify
        // Step 2 of checkout: Razorpay's popup already succeeded on the frontend.
        // This is the REAL security step - verify the signature genuinely came
        // from Razorpay, THEN commit stock reduction + order creation as one transaction.
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment(VerifyPaymentDto dto)
        {
            var userId = GetCurrentUserId();
            var keySecret = _config["Razorpay:KeySecret"]!;

            var attributes = new Dictionary<string, string> 
            {
                { "razorpay_order_id", dto.RazorpayOrderId },
                { "razorpay_payment_id", dto.RazorpayPaymentId },
                { "razorpay_signature", dto.RazorpaySignature }
            };

            try
            {
                attributes["razorpay_signature"] = dto.RazorpaySignature;
                Utils.verifyPaymentSignature(attributes);
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Payment verification failed." });
            }

            // Signature is genuine - now commit everything as ONE transaction.
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var cartItems = await _db.CartItems
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .ToListAsync();

                if (cartItems.Count == 0)
                    return BadRequest(new { message = "Cart is empty." });

                var order = new EcommerceApi.Models.Order
                {
                    UserId = userId,
                    Status = "Paid",
                    RazorpayOrderId = dto.RazorpayOrderId,
                    RazorpayPaymentId = dto.RazorpayPaymentId,
                    TotalAmount = cartItems.Sum(c => c.Product!.Price * c.Quantity)
                };

                foreach (var item in cartItems)
                {
                    if (item.Product!.Quantity < item.Quantity)
                        throw new Exception($"Not enough stock for {item.Product.Name}");

                    item.Product.Quantity -= item.Quantity;

                    order.Items.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        Price = item.Product.Price,
                        Quantity = item.Quantity
                    });
                }

                _db.Orders.Add(order);
                _db.CartItems.RemoveRange(cartItems);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Payment verified, order placed.", orderId = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/order/my-orders
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetCurrentUserId();

            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    Items = o.Items.Select(i => new OrderItemResponseDto
                    {
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