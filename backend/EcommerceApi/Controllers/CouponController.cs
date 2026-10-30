using System.Security.Claims;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CouponController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICouponService _coupons;

        public CouponController(AppDbContext db, ICouponService coupons)
        {
            _db = db;
            _coupons = coupons;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /api/coupon/validate  { code }   -> a PREVIEW only, nothing is consumed
        [HttpPost("validate")]
        [EnableRateLimiting("coupon")]
        public async Task<IActionResult> Validate(ValidateCouponDto dto)
        {
            var userId = CurrentUserId;
            var cart = await _db.CartItems.Where(c => c.UserId == userId).Include(c => c.Product).ToListAsync();
            if (cart.Count == 0) return BadRequest(new { message = "Your cart is empty." });

            var subTotal = cart.Sum(c => c.Product!.Price * c.Quantity);
            var result = await _coupons.EvaluateAsync(dto.Code, userId, subTotal);

            return Ok(new CouponPreviewDto
            {
                Valid = result.Success,
                Message = result.Message,
                Code = result.Coupon?.Code,
                SubTotal = result.SubTotal,
                Discount = result.Discount,
                Total = result.Total
            });
        }

        // ---------- Admin ----------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CouponResponseDto
                {
                    Id = c.Id, Code = c.Code, DiscountType = c.DiscountType, Value = c.Value,
                    MaxDiscount = c.MaxDiscount, MinOrderAmount = c.MinOrderAmount,
                    StartsAt = c.StartsAt, ExpiresAt = c.ExpiresAt,
                    UsageLimit = c.UsageLimit, UsedCount = c.UsedCount, IsActive = c.IsActive
                })
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CouponDto dto)
        {
            if (dto.DiscountType != "Percentage" && dto.DiscountType != "Fixed")
                return BadRequest(new { message = "Type must be Percentage or Fixed." });
            if (dto.DiscountType == "Percentage" && dto.Value > 100)
                return BadRequest(new { message = "A percentage cannot be more than 100." });
            if (dto.StartsAt.HasValue && dto.ExpiresAt.HasValue && dto.ExpiresAt <= dto.StartsAt)
                return BadRequest(new { message = "Expiry must be after the start date." });

            var code = dto.Code.Trim().ToUpperInvariant();
            if (await _db.Coupons.AnyAsync(c => c.Code == code))
                return Conflict(new { message = "A coupon with this code already exists." });

            _db.Coupons.Add(new Coupon
            {
                Code = code, DiscountType = dto.DiscountType, Value = dto.Value,
                MaxDiscount = dto.MaxDiscount, MinOrderAmount = dto.MinOrderAmount,
                StartsAt = dto.StartsAt, ExpiresAt = dto.ExpiresAt,
                UsageLimit = dto.UsageLimit, IsActive = dto.IsActive
            });
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Coupon {code} created." });
        }

        // Deactivate instead of delete, so redemption history keeps its meaning
        [HttpPost("{id:int}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Toggle(int id)
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id);
            if (coupon == null) return NotFound(new { message = "Coupon not found." });

            coupon.IsActive = !coupon.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { message = coupon.IsActive ? "Coupon activated." : "Coupon deactivated." });
        }
    }
}