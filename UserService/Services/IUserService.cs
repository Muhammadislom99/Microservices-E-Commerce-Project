using UserService.Models.DTOs;

namespace UserService.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<bool> DeleteUserAsync(int id);
}