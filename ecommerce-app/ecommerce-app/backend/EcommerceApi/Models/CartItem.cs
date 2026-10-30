namespace EcommerceApi.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        // Which logged-in user this cart row belongs to
        public int UserId { get; set; }

        // Which product they added
        public int ProductId { get; set; }
        public Product? Product { get; set; }   // navigation property - EF Core auto-loads the related Product

        public int Quantity { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
