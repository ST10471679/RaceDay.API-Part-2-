using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UsersController(ApplicationDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    // GET api/users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserProfileResponseDto>> GetUser(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        return Ok(ToDto(user));
    }

    // PUT api/users/5  (multipart/form-data: UserName, ProfilePicture)
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UserProfileResponseDto>> UpdateUser(int id, [FromForm] UpdateUserProfileDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        var fullName = (dto.UserName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "Name cannot be empty." });

        // "Jane van Wyk" -> FirstName "Jane", LastName "van Wyk"
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        user.FirstName = parts[0];
        user.LastName = parts.Length > 1 ? parts[1] : string.Empty;

        if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
        {
            try
            {
                user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(dto.ProfilePicture, "profiles");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    private static UserProfileResponseDto ToDto(RaceDay.API.Models.User user) => new()
    {
        Id = user.UserId,
        Email = user.Email,
        UserName = $"{user.FirstName} {user.LastName}".Trim(),
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}
