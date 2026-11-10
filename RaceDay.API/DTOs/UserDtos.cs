namespace RaceDay.API.DTOs;

public class UserProfileResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

// Bound from multipart/form-data
public class UpdateUserProfileDto
{
    public string UserName { get; set; } = string.Empty;
    public IFormFile? ProfilePicture { get; set; }
}
