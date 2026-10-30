namespace EcommerceApi.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        // Price stored at time of purchase - protects the order record
        // even if the product's price later changes.
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}