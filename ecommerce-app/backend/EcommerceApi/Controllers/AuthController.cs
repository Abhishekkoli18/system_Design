using EcommerceApi.DTOs;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Mvc;
using EcommerceApi.Services.Logging;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]   // becomes /api/auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IApplicationLogger _appLogger;   // ← add this

        public AuthController(IAuthService authService, IApplicationLogger appLogger)   // ← and this
        {
            _authService = authService;
            _appLogger = appLogger;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (result == null)
                return BadRequest(new { message = "Email is already registered." });

            return Ok(result);
        }

        // POST /api/auth/login
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // await _appLogger.Log($"Login attempt for {dto.Email}");
            var result = await _authService.LoginAsync(dto);
            if (result == null)
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(result);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
        {
            var result = await _authService.GoogleLoginAsync(dto.IdToken);
            if (result == null)
                return Unauthorized(new { message = "Google sign-in failed." });

            return Ok(result);
        }
    }
}
