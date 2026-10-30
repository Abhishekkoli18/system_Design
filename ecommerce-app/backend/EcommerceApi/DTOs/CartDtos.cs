namespace EcommerceApi.DTOs
{
    // What the frontend sends when adding an item to the cart.
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    // What we send back when returning the cart contents -
    // combines CartItem + Product info so the frontend doesn't
    // need a second request just to show the product name/price.
    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
