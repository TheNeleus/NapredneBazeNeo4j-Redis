using Microsoft.AspNetCore.Mvc;
using MeetupBackend.Models;
using MeetupBackend.Services;
using MeetupBackend.DTOs;

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
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            var user = new User
            {
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                Interests = createUserDto.Interests,
                Bio = createUserDto.Bio
            };

            try
            {
                var createdUser = await _userService.CreateUser(user);
                return CreatedAtAction(nameof(CreateUser), new { id = createdUser.Id }, MapToDto(createdUser));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userService.GetUserByEmail(loginDto.Email);
            if (user == null) return Unauthorized("User not found.");

            string token = await _userService.CreateSession(user.Id);
            
            return Ok(new { Token = token, User = MapToDto(user) });
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
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updateDto)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            string? sessionUserId = await _userService.GetUserIdFromSession(token);

            if (sessionUserId == null || sessionUserId != id)
            {
                return Unauthorized("Invalid session or user mismatch.");
            }

            var userUpdates = new User
            {
                Id = id,
                Name = updateDto.Name ?? string.Empty,
                Email = updateDto.Email ?? string.Empty,
                Bio = updateDto.Bio ?? string.Empty,
                Interests = updateDto.Interests ?? new List<string>()
            };

            var updatedUser = await _userService.UpdateUser(id, userUpdates);

            if (updatedUser != null) 
            {
                return Ok(MapToDto(updatedUser));
            }
            
            return NotFound("User not found.");
        }
        
        [HttpPost("{id}/friend")]
        public async Task<IActionResult> AddFriend(string id, [FromBody] AddFriendDto dto)
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            string? sessionUserId = await _userService.GetUserIdFromSession(token);

            if (sessionUserId == null || sessionUserId != id) return Unauthorized("Unauthorized.");

            var result = await _userService.AddFriendByEmail(id, dto.Email.Trim());

            if (result == "User not found") return NotFound("User with that email does not exist.");
            if (result == "Cannot add yourself") return BadRequest("You cannot add yourself.");
            
            return Ok(new { message = "Friend added successfully!" });
        }

        private UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Interests = user.Interests,
                Bio = user.Bio,
                Role = user.Role
            };
        }
    }
}