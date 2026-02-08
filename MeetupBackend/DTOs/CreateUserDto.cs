using System.ComponentModel.DataAnnotations;

namespace MeetupBackend.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public List<string> Interests { get; set; } = new List<string>();

        public string Bio { get; set; } = string.Empty;
    }
}