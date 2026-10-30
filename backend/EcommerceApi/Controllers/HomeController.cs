using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // requires a valid JWT for ALL endpoints in this controller
    public class HomeController : ControllerBase
    {
        // GET /api/home/user
        // Any logged-in user (User or Admin) can access this.
        [HttpGet("user")]
        public IActionResult UserHome()
        {
            var name = User.Identity?.Name;
            return Ok(new { message = $"Welcome, {name}! This is the user home page." });
        }

        // GET /api/home/admin
        // Only accounts with Role = "Admin" (from the JWT claim) can access this.
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminHome()
        {
            var name = User.Identity?.Name;
            return Ok(new { message = $"Welcome, {name}! This is the admin dashboard." });
        }
    }
}
