namespace MeetupBackend.Models
{
    public class User
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new List<string>();

        public string Role { get; set; } = "User";
    }
}