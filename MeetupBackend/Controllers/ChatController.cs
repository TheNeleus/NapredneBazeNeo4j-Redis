using MeetupBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeetupBackend.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("{eventId}/history")]
        public async Task<IActionResult> GetHistory(string eventId, [FromQuery] int page = 0)
        {
            var history = await _chatService.GetEventHistory(eventId, page);
            return Ok(history);
        }
    }
}