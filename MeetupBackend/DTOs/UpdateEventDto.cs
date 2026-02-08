using System.ComponentModel.DataAnnotations;

namespace MeetupBackend.DTOs
{
    public class UpdateEventDto
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? Date { get; set; }

        public string? Category { get; set; }

        [Range(-90.0, 90.0)]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0)]
        public double? Longitude { get; set; }
    }
}