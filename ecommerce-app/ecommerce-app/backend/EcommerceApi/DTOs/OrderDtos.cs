namespace EcommerceApi.DTOs
{
    // Sent back after creating a Razorpay order - frontend uses this to open the widget
    public class CreateOrderResponseDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
        public int AmountInPaise { get; set; }
        public string Currency { get; set; } = "INR";
    }

    // What the frontend sends back after Razorpay's popup succeeds
    public class VerifyPaymentDto
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }

    public class OrderItemResponseDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}