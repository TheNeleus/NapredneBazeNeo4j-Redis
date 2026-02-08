namespace MeetupBackend.DTOs
{
    public class EventResponseDto
    {
        public string? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> Attendees { get; set; } = new List<string>();
        public string CreatorId { get; set; } = string.Empty;
        
        // Optional recommendation fields
        public double? DistanceKm { get; set; }
        public int? FriendsGoing { get; set; }
    }
}