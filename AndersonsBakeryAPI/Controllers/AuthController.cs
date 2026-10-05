using System;
using System.Threading.Tasks;
using AndersonsBakeryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginServiceRouter _loginRouter;

        public AuthController(LoginServiceRouter loginRouter)
        {
            _loginRouter = loginRouter;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public ActionResult<UserRow> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            var user = _loginRouter.Authenticate(request.Username, request.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password." });

            return Ok(user);
        }
    }
}

