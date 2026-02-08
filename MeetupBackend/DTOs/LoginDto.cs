using System.ComponentModel.DataAnnotations;

namespace MeetupBackend.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}