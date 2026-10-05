using System;
using System.Collections.Generic;
using AndersonsBakeryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserServiceRouter _userRouter;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserServiceRouter userRouter, ILogger<UsersController> logger)
        {
            _userRouter = userRouter;
            _logger = logger;
        }

        public class CreateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        public class UpdateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string? Password { get; set; }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<IEnumerable<UserRow>> GetAllUsers()
        {
            _logger.LogInformation("GET /api/users called");
            var users = _userRouter.GetAllUsers();
            return Ok(users);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            if (_userRouter.UsernameExists(request.Username))
                return Conflict(new { message = "Username already taken." });

            try
            {
                _logger.LogInformation("Creating user: {Username} with role {Role}", request.Username, request.Role);
                _userRouter.AddUser(request.Username, request.Password, request.Role);
                return StatusCode(201, new { message = "User created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", request.Username);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(id) || request == null || string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { message = "Invalid user update payload." });

            if (_userRouter.UsernameExists(request.Username, id))
                return Conflict(new { message = "Username already taken." });

            try
            {
                _logger.LogInformation("Updating user: {Id}", id);
                _userRouter.UpdateUser(id, request.Username, request.Role, request.Password);
                return Ok(new { message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "User id is required." });

            try
            {
                _logger.LogInformation("Deleting user: {Id}", id);
                _userRouter.DeleteUser(id);
                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}


