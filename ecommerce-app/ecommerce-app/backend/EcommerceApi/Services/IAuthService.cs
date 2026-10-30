using EcommerceApi.DTOs;

namespace EcommerceApi.Services
{
    // An interface = a contract. AuthController only depends on THIS,
    // not the concrete AuthService class. This is what makes dependency
    // injection and unit testing easier later.
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<AuthResponseDto?> GoogleLoginAsync(string idToken);
    }
}
