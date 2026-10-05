using System;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AndersonsBakeryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginServiceRouter _loginRouter;
        private readonly IConfiguration _configuration;

        public AuthController(LoginServiceRouter loginRouter, IConfiguration configuration)
        {
            _loginRouter = loginRouter;
            _configuration = configuration;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public ActionResult<object> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            var user = _loginRouter.Authenticate(request.Username, request.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password." });

            // Create JWT token
            var jwt = _configuration.GetSection("Jwt");
            var key = jwt["Key"] ?? string.Empty;
            if (key.Length < 16)
                return StatusCode(500, new { message = "JWT signing key is not configured or too short." });
            var issuer = jwt["Issuer"];
            var audience = jwt["Audience"];
            var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 60;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? user.Name ?? string.Empty),
                new Claim("username", user.Name ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature),
                Issuer = issuer,
                Audience = audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Project to a safe shape — never return the full UserRow (hashed password would be included)
            return Ok(new
            {
                token = tokenString,
                user = new { id = user.Id, sqlId = user.SqlId, name = user.Name, role = user.Role }
            });
        }
    }
}

