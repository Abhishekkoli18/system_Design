namespace EcommerceApi.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }

        // "Pending" until payment is verified, then "Paid" or "Failed"
        public string Status { get; set; } = "Pending";

        // Razorpay's own reference IDs, stored once payment is verified
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal SubTotal { get; set; }          // before the discount
        public decimal DiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        public int? CouponId { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}