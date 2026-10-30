namespace EcommerceApi.Models
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;            // always stored UPPERCASE
        public string DiscountType { get; set; } = "Percentage";    // "Percentage" or "Fixed"
        public decimal Value { get; set; }                          // 10 means 10% or Rs 10
        public decimal? MaxDiscount { get; set; }                   // cap for percentage coupons
        public decimal MinOrderAmount { get; set; }
        public DateTime? StartsAt { get; set; }                     // UTC
        public DateTime? ExpiresAt { get; set; }                    // UTC
        public int? UsageLimit { get; set; }                        // total uses, null = unlimited
        public int UsedCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}