namespace EcommerceApi.DTOs
{
    public class CreateOrderRequestDto
    {
        public string? CouponCode { get; set; }
    }

    public class CreateOrderResponseDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
        public int AmountInPaise { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    public class VerifyPaymentDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }

    public class OrderItemResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}