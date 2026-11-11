namespace RaceDay.Web.Models.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }

    public class UpdateProfileDto
    {
        public string UserName { get; set; } = string.Empty;
        public IFormFile? ProfilePicture { get; set; }
    }
}