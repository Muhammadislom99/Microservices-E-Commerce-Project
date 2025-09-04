namespace OrderService.Models.DTOs;

public class CreateOrderDto
{
    public int UserId { get; set; }
    public List<CreateOrderDetailDto> OrderDetails { get; set; } = new();
}