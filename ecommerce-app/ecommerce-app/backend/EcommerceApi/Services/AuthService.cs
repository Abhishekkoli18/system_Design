using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Helpers;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth;

namespace EcommerceApi.Services
{
    // This is where the real work happens. The controller stays "thin" -
    // it just receives the HTTP request and calls this service.
    public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, JwtHelper jwtHelper, ILogger<AuthService> logger)
    {
        _db = db;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        _logger.LogInformation("Registration attempt for {Email}", dto.Email);

        // Check if email is already taken
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existing != null)
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", dto.Email);
            return null; // controller will turn this into a 400 response
        }

        // Hash the password - NEVER store plain text passwords.
        // BCrypt automatically handles salting internally.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            // Every public registration is a "User" - no exceptions.
            // The one Admin account is hardcoded and seeded separately
            // at startup (see Data/DbSeeder.cs), never created here.
            Role = "User"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtHelper.GenerateToken(user);

        _logger.LogInformation("Registration succeeded for {Email}", dto.Email);

        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        _logger.LogInformation("Login attempt for {Email}", dto.Email);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed - no user found for {Email}", dto.Email);
            return null;
        }

        // Compare the plain password the user typed against the stored hash.
        bool isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isValid)
        {
            _logger.LogWarning("Login failed - wrong password for {Email}", dto.Email);
            return null;
        }

        var token = _jwtHelper.GenerateToken(user);

        _logger.LogInformation("Login succeeded for {Email}, role {Role}", dto.Email, user.Role);

        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role
        };
    }
    public async Task<AuthResponseDto?> GoogleLoginAsync(string idToken)
    {
        // This line is the actual security check - it verifies the token
        // was genuinely issued by Google and hasn't been tampered with.
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
        }
        catch (InvalidJwtException)
        {
            return null; // token was fake or expired
        }

        var email = payload.Email;
        var fullName = payload.Name;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // First time this Google account has been used here - auto-register them.
            // They never typed a password, so we store a random unusable hash -
            // they'll always sign in via Google, never via the password form.
            user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var token = _jwtHelper.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
}
