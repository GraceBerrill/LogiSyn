using System;
using System.Collections.Generic;
using AndersonsBakeryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Model;

namespace AndersonsBakeryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserServiceRouter _userRouter;

        public UsersController(UserServiceRouter userRouter)
        {
            _userRouter = userRouter;
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
        public ActionResult<IEnumerable<UserRow>> GetAllUsers()
        {
            var users = _userRouter.GetAllUsers();
            return Ok(users);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            if (_userRouter.UsernameExists(request.Username))
                return Conflict(new { message = "Username already taken." });

            try
            {
                _userRouter.AddUser(request.Username, request.Password, request.Role);
                return Ok(new { message = "User created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(id) || request == null || string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { message = "Invalid user update payload." });

            if (_userRouter.UsernameExists(request.Username, id))
                return Conflict(new { message = "Username already taken." });

            try
            {
                _userRouter.UpdateUser(id, request.Username, request.Role, request.Password);
                return Ok(new { message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "User id is required." });

            try
            {
                _userRouter.DeleteUser(id);
                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

