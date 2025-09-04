using UserService.Models.DTOs;

namespace UserService.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
    string GenerateJwtToken(int userId, string username);
}

