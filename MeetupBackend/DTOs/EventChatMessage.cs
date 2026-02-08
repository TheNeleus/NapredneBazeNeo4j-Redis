namespace MeetupBackend.DTOs
{
    public class EventChatMessage
    {
        public string Id { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public EventChatMessage()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }
    }
}