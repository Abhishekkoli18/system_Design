using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class ValidateCouponDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class CouponPreviewDto
    {
        public bool Valid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    // Admin create
    public class CouponDto
    {
        [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
        [Required] public string DiscountType { get; set; } = "Percentage";
        [Range(0.01, 1000000)] public decimal Value { get; set; }
        public decimal? MaxDiscount { get; set; }
        [Range(0, 100000000)] public decimal MinOrderAmount { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        [Range(1, 1000000)] public int? UsageLimit { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CouponResponseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal MinOrderAmount { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }
    }
}