using System.ComponentModel.DataAnnotations;

namespace MeetupBackend.DTOs
{
    public class UpdateUserDto
    {
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public List<string>? Interests { get; set; }

        public string? Bio { get; set; }
    }
}