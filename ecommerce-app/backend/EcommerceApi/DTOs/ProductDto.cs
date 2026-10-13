using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    // What the Admin's "manual add product" form sends.
    public class ProductDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or more")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or more")]
        public int Quantity { get; set; }
    }

    // What we send back to the frontend when listing products.
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
