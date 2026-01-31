using Microsoft.AspNetCore.Mvc;
using MeetupBackend.Models;
using MeetupBackend.Services;

namespace MeetupBackend.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventController : ControllerBase
    {
        private readonly EventService _eventService;
        private readonly UserService _userService;

        public EventController(EventService eventService, UserService userService)
        {
            _eventService = eventService;
            _userService = userService;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] Event evt, [FromHeader] string token)
        {
            string? userId = await _userService.GetUserIdFromSession(token);

            if (userId == null)
            {
                return Unauthorized("Your session expired. Log in again.");
            }

            await _eventService.CreateEvent(evt, userId);

            return Ok("Event is created!");
        }

        [HttpPost("{eventId}/attend")]
        public async Task<IActionResult> AttendEvent(string eventId, [FromHeader] string token)
        {
            string? userId = await _userService.GetUserIdFromSession(token);

            if (userId == null)
            {
                return Unauthorized("Session has expired.");
            }

            await _eventService.AttendEvent(userId, eventId.Trim());
            return Ok("Attending successful!");
        }

        [HttpDelete("{eventId}")]
        public async Task<IActionResult> DeleteEvent(string eventId, [FromHeader] string token)
        {
            string? userId = await _userService.GetUserIdFromSession(token);
            if (userId == null) return Unauthorized();

            bool success = await _eventService.DeleteEvent(userId, eventId);

            if (success)
            {
                return Ok("Event successfully deleted.");
            }
            else
            {
                // Ne znamo dal je bad request il not found, nbt vrv
                return BadRequest("Event not found or you don't have permission to delete it.");
            }
        }

        [HttpGet("friendsrecommendations")]
        public async Task<IActionResult> GetFriendsEvents([FromHeader] string token)
        {
            string? userId = await _userService.GetUserIdFromSession(token);

            if (userId == null) return Unauthorized("Session expired.");

            var events = await _eventService.GetFriendsEvents(userId);
            return Ok(events);
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations(
            [FromHeader] string token, 
            [FromQuery] double latitude, 
            [FromQuery] double longitude,
            [FromQuery] double radius = 10)
        {
            string? userId = await _userService.GetUserIdFromSession(token);

            if (userId == null) return Unauthorized("Session expired.");

            var events = await _eventService.GetRecommendedEvents(userId, latitude, longitude, radius);
            return Ok(events);
        }

        [HttpGet("nearest")]
        public async Task<IActionResult> GetNearestEvent(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius = 50)
        {
            if(latitude == 0 || longitude == 0)
            {
                return BadRequest("Latitude and Longidude are required.");
            }

            var events = await _eventService.GetNearestEvents(latitude, longitude, radius);

            return Ok(events);
        }

        [HttpPut("{eventId}")]
        public async Task<IActionResult> UpdateEvent(
            string eventId,
            [FromBody] Event evt, 
            [FromHeader] string token)
        {
            string? userId = await _userService.GetUserIdFromSession(token);

            if (userId == null) return Unauthorized("Session expired.");

            var updatedEvent = await _eventService.UpdateEvent(userId, eventId, evt);

            if (updatedEvent != null)
            {
                return Ok(updatedEvent);
            }
            else
            {
                return BadRequest("Event not found or you don't have permission to update it.");
            }
        }
    }
}