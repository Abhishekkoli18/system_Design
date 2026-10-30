using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]   // becomes /api/cart
    [Authorize]                   // any logged-in user - User or Admin
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext db, ILogger<CartController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Reads the user's ID out of the JWT token - the same claim
        // JwtHelper embedded at login (ClaimTypes.NameIdentifier).
        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // GET /api/cart - the logged-in user's own cart contents
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetCurrentUserId();

            var items = await _db.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product) // joins Product data in the same query
                .Select(c => new CartItemResponseDto
                {
                    CartItemId = c.Id,
                    ProductId = c.ProductId,
                    Name = c.Product!.Name,
                    ImageUrl = c.Product.ImageUrl,
                    Price = c.Product.Price,
                    Quantity = c.Quantity
                })
                .ToListAsync();

            return Ok(items);
        }

        // POST /api/cart/add
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(AddToCartDto dto)
        {
            var userId = GetCurrentUserId();

            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (dto.Quantity < 1)
                return BadRequest(new { message = "Quantity must be at least 1." });

            if (dto.Quantity > product.Quantity)
                return BadRequest(new { message = $"Only {product.Quantity} in stock." });

            // If this product is already in the cart, increase the quantity
            // instead of creating a duplicate row.
            var existing = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId);

            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} added Product {ProductId} to cart, quantity {Quantity}", userId, dto.ProductId, dto.Quantity);

            return Ok(new { message = "Added to cart." });
        }

        // DELETE /api/cart/remove/5
        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = GetCurrentUserId();

            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item == null)
                return NotFound(new { message = "Cart item not found." });

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Removed from cart." });
        }
    }
}
