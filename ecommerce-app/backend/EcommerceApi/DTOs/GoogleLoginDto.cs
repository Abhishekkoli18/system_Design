namespace EcommerceApi.DTOs
{
    // What the frontend sends after Google sign-in succeeds -
    // just the raw token Google gave the browser.
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}