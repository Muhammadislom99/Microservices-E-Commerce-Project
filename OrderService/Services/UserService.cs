using System.Text.Json;
using OrderService.Models.DTOs;

namespace OrderService.Services;

public class UserService(HttpClient httpClient) : IUserService
{
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}