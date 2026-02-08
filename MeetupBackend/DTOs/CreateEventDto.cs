using System.ComponentModel.DataAnnotations;

namespace MeetupBackend.DTOs
{
    public class CreateEventDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Range(-90.0, 90.0)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180.0, 180.0)]
        public double Longitude { get; set; }
    }
}