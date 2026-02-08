using MeetupBackend.Models;
using MeetupBackend.Services;
using Microsoft.AspNetCore.SignalR;
using MeetupBackend.DTOs;

namespace MeetupBackend.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly UserService _userService;

        public ChatHub(ChatService chatService, UserService userService)
        {
            _chatService = chatService;
            _userService = userService;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var token = httpContext?.Request.Query["access_token"].ToString();
            
            if (!string.IsNullOrEmpty(token))
            {
                var userId = await _userService.GetUserIdFromSession(token);
                if (userId != null)
                {
                    var user = await _userService.GetUserById(userId);
                    Context.Items["User"] = user; 
                }
            }
            
            await base.OnConnectedAsync();
        }

        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, eventId);
        }

        public async Task LeaveEventGroup(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId);
        }

        public async Task SendMessageToEvent(string eventId, string content)
        {
            var user = Context.Items["User"] as User;
            if (user == null) return;

            var msg = new EventChatMessage()
            {
                EventId = eventId,
                SenderId = user.Id,
                SenderName = user.Name,
                Content = content
            };
            
            await _chatService.SendMessage(msg);
        }
    }
}