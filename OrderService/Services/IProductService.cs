using OrderService.Models.DTOs;

namespace OrderService.Services;

public interface IProductService
{
    Task<ProductDto?> GetProductByIdAsync(int productId);
}