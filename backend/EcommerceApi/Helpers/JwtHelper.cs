using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcommerceApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceApi.Helpers
{
    // This class builds the actual JWT (JSON Web Token) after a successful login.
    // The token is a signed string the frontend stores and sends back on every
    // future request, proving "I am this user, with this role" without the
    // server needing to look anything up in the database each time.
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            // Claims = pieces of information embedded inside the token.
            // The Role claim is what lets [Authorize(Roles = "Admin")] work later.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = double.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
