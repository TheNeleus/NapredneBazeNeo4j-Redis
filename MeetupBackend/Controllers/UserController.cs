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
        public async Task<IActionResult> AddFriend(string friendId, [FromHeader] string token)
        {
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
            if (user == null)
            {
                return Unauthorized("There's no user with this email.");
            }

            string token = await _userService.CreateSession(user.Id);

            return Ok(new { Token = token, User = user });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromHeader] string token)
        {
            await _userService.Logout(token);
            return Ok("Logout successful.");
        }
    }
}