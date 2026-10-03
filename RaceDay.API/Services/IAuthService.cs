using RaceDay.API.DTOs;

namespace RaceDay.API.Services;

public interface IAuthService
{
    Task<UserResponseDto?> RegisterAsync(RegisterDto dto);
    Task<UserResponseDto?> LoginAsync(LoginDto dto);
}