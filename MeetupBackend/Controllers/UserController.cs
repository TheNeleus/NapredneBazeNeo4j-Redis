using Microsoft.AspNetCore.Mvc;
using MeetupBackend.Models;
using MeetupBackend.Services;

namespace MeetupBackend.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            var createdUser = await _userService.CreateUser(user);
            return CreatedAtAction(nameof(CreateUser), new { id = createdUser.Id }, createdUser);
        }

        [HttpPost("friends/{friendId}")]
        public async Task<IActionResult> AddFriend(string friendId)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            string? currentUserId = await _userService.GetUserIdFromSession(token);

            if (currentUserId == null)
            {
                return Unauthorized("Sesija je istekla.");
            }

            await _userService.AddFriend(currentUserId, friendId.Trim());
            return Ok("Friend added successfully!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] string email)
        {
            var user = await _userService.GetUserByEmail(email);
            if (user == null) return Unauthorized("User not found.");

            string token = await _userService.CreateSession(user.Id);
            return Ok(new { Token = token, User = user });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            if (!string.IsNullOrEmpty(token))
            {
                await _userService.Logout(token);
            }
            return Ok("Logout successful.");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] User userUpdates)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            string? userId = await _userService.GetUserIdFromSession(token);

            if (userId == null)
            {
                return Unauthorized("Session expired.");
            }

            userUpdates.Id = userId;

            var updatedUser = await _userService.UpdateUser(userId, userUpdates);

            if (updatedUser != null) return Ok(updatedUser);
            return NotFound("User not found.");
        }
    }
}