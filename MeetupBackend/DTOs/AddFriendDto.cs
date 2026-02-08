using System.ComponentModel.DataAnnotations;

namespace MeetupBackend.DTOs
{
    public class AddFriendDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}