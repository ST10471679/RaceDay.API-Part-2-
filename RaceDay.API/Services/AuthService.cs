using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    

    public AuthService(ApplicationDbContext context)

    {
        _context = context;
       
    }

    public async Task<UserResponseDto?> RegisterAsync(RegisterDto dto)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return null;
        }

        // Only the seeded roles are valid (1 = Organiser, 2 = Participant); anything else becomes Participant
        int roleId = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId) ? dto.RoleId : 2;

        // Hash the password securely with BCrypt
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = hashedPassword,
            RoleId = roleId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Fetch user with Role included for response
        var createdUser = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        return new UserResponseDto
        {
            UserId = createdUser!.UserId,
            FirstName = createdUser.FirstName,
            LastName = createdUser.LastName,
            Email = createdUser.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            RoleName = createdUser.Role?.RoleName ?? "Participant",
            Token = "temporary-token"
        };
    }

    public async Task<UserResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            return null;
        }

        // Verify password hash
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return null;
        }

        return new UserResponseDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}", // Or user.FirstName + " " + user.LastName
            RoleName = user.Role?.RoleName ?? "Participant",
            Token = "temporary-token"
        };
    }
}