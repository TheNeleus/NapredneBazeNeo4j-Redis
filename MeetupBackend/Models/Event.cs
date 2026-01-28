namespace MeetupBackend.Models
{
    public class Event
    {
        public string? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}