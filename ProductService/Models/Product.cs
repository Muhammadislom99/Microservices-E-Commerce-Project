using System.ComponentModel.DataAnnotations;

namespace ProductService.Models;

public class Product
{
    public int Id { get; set; }
        
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
        
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
        
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
        
    public int Stock { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}