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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User userUpdates)
        {
            
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            string? sessionUserId = await _userService.GetUserIdFromSession(token);

            if (sessionUserId == null || sessionUserId != id)
            {
                return Unauthorized("Invalid session or user mismatch.");
            }

            userUpdates.Id = id; 

            var updatedUser = await _userService.UpdateUser(id, userUpdates);

            if (updatedUser != null) return Ok(updatedUser);
            return NotFound("User not found.");
        }
        
        [HttpPost("{id}/friend")]
        public async Task<IActionResult> AddFriend(string id, [FromBody] Dictionary<string, string> body)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            string? sessionUserId = await _userService.GetUserIdFromSession(token);

            if (sessionUserId == null || sessionUserId != id) return Unauthorized("Unauthorized.");

            if (body == null || !body.ContainsKey("email"))
            {
                return BadRequest("Email is required in request body.");
            }

            string email = body["email"];

            if (string.IsNullOrEmpty(email)) return BadRequest("Email cannot be empty.");


            var result = await _userService.AddFriendByEmail(id, email.Trim());

            if (result == "User not found") return NotFound("User with that email does not exist.");
            if (result == "Cannot add yourself") return BadRequest("You cannot add yourself.");
            
            return Ok(new { message = "Friend added successfully!" });
        }
    }
}