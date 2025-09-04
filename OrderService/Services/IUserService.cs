using OrderService.Models.DTOs;

namespace OrderService.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId);
}