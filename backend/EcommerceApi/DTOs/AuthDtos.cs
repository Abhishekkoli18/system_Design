using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    // What the frontend sends when registering.
    // No "Role" field - every public registration becomes a "User".
    // The Admin account is a separate, hardcoded account created at startup
    // (see Data/DbSeeder.cs) - never created through this public endpoint.
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        // [EmailAddress] is a built-in validation attribute - ASP.NET Core checks
        // this automatically before the controller method runs, and returns a
        // 400 Bad Request if the format is invalid. This is the SERVER-SIDE half
        // of the same check done in the React form - never trust frontend-only validation.
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    // What the frontend sends when logging in.
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // What the backend sends back after a successful login/register.
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
