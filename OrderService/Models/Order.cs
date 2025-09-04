using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }
        
    [Required]
    public int UserId { get; set; }
        
    public decimal TotalAmount { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    public List<OrderDetail> OrderDetails { get; set; } = new();
}