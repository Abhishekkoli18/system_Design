namespace EcommerceApi.Models
{
    public class CouponRedemption
    {
        public int Id { get; set; }
        public int CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
    }
}