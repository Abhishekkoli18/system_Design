using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services.Coupons
{
    public record CouponResult(bool Success, string Message, Coupon? Coupon, decimal SubTotal, decimal Discount)
    {
        public decimal Total => SubTotal - Discount;
    }

    public interface ICouponService
    {
        Task<CouponResult> EvaluateAsync(string? code, int userId, decimal subTotal);
    }

    public class CouponService : ICouponService
    {
        private readonly AppDbContext _db;
        public CouponService(AppDbContext db) => _db = db;

        public async Task<CouponResult> EvaluateAsync(string? code, int userId, decimal subTotal)
        {
            CouponResult Fail(string message) => new(false, message, null, subTotal, 0);

            if (string.IsNullOrWhiteSpace(code))
                return new CouponResult(true, string.Empty, null, subTotal, 0);   // no coupon is fine

            var normalized = code.Trim().ToUpperInvariant();
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == normalized);
            var now = DateTime.UtcNow;

            if (coupon == null || !coupon.IsActive) return Fail("Invalid coupon code.");
            if (coupon.StartsAt.HasValue && now < coupon.StartsAt.Value) return Fail("This coupon is not active yet.");
            if (coupon.ExpiresAt.HasValue && now > coupon.ExpiresAt.Value) return Fail("This coupon has expired.");
            if (subTotal < coupon.MinOrderAmount) return Fail($"Minimum order amount is ₹{coupon.MinOrderAmount:0.##}.");
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value) return Fail("This coupon has been fully used.");
            if (await _db.CouponRedemptions.AnyAsync(r => r.CouponId == coupon.Id && r.UserId == userId))
                return Fail("You have already used this coupon.");

            var discount = coupon.DiscountType == "Percentage"
                ? Math.Round(subTotal * coupon.Value / 100m, 2, MidpointRounding.AwayFromZero)
                : coupon.Value;

            if (coupon.MaxDiscount.HasValue) discount = Math.Min(discount, coupon.MaxDiscount.Value);
            discount = Math.Min(discount, subTotal - 1m);      // keep at least ₹1 payable, Razorpay needs a positive amount
            if (discount < 0) discount = 0;

            return new CouponResult(true, $"Coupon {coupon.Code} applied.", coupon, subTotal, discount);
        }
    }
}