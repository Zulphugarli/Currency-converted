using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CurrencyConvertorApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("token")]
        public IActionResult GenerateToken([FromBody] UserLogin login)
        {
            if (IsValidUser(login, out string role))
            {
                var jwtKey = _configuration.GetValue<string>("Jwt:Key") ?? "super_secret_jwt_key!";
                var jwtIssuer = _configuration.GetValue<string>("Jwt:Issuer") ?? "currency-api";
                var jwtAudience = _configuration.GetValue<string>("Jwt:Audience") ?? "currency-clients";

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, login.Username),
                    new Claim(ClaimTypes.Role, role)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token)
                });
            }

            return Unauthorized("Invalid username or password");
        }

        private bool IsValidUser(UserLogin login, out string role)
        {
            if (login.Username == "admin" && login.Password == "123")
            {
                role = "Admin";
                return true;
            }

            if (login.Username == "user" && login.Password == "123")
            {
                role = "User";
                return true;
            }

            role = string.Empty;
            return false;
        }
    }

    public record UserLogin(string Username, string Password);
}
