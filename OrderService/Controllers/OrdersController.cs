using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Models.DTOs;
using OrderService.Services;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(OrderDbContext context, IUserService userService, IProductService productService)
        : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var orders = await context.Orders
                .Include(o => o.OrderDetails)
                .ToListAsync();

            var orderDtos = new List<OrderDto>();

            foreach (var order in orders)
            {
                var user = await userService.GetUserByIdAsync(order.UserId);
                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    UserName = user?.Username ?? "Unknown",
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    OrderDetails = new List<OrderDetailDto>()
                };

                foreach (var detail in order.OrderDetails)
                {
                    var product = await productService.GetProductByIdAsync(detail.ProductId);
                    orderDto.OrderDetails.Add(new OrderDetailDto
                    {
                        Id = detail.Id,
                        ProductId = detail.ProductId,
                        ProductName = product?.Name ?? "Unknown",
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        TotalPrice = detail.TotalPrice
                    });
                }

                orderDtos.Add(orderDto);
            }

            return Ok(orderDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var user = await userService.GetUserByIdAsync(order.UserId);
            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = user?.Username ?? "Unknown",
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                OrderDetails = new List<OrderDetailDto>()
            };

            foreach (var detail in order.OrderDetails)
            {
                var product = await productService.GetProductByIdAsync(detail.ProductId);
                orderDto.OrderDetails.Add(new OrderDetailDto
                {
                    Id = detail.Id,
                    ProductId = detail.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    TotalPrice = detail.TotalPrice
                });
            }

            return Ok(orderDto);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            // Validate user exists
            var user = await userService.GetUserByIdAsync(createOrderDto.UserId);
            if (user == null)
                return BadRequest("User not found");

            var order = new Order
            {
                UserId = createOrderDto.UserId,
                OrderDetails = new List<OrderDetail>()
            };

            decimal totalAmount = 0;

            foreach (var detailDto in createOrderDto.OrderDetails)
            {
                var product = await productService.GetProductByIdAsync(detailDto.ProductId);
                if (product == null)
                    return BadRequest($"Product with ID {detailDto.ProductId} not found");

                if (product.Stock < detailDto.Quantity)
                    return BadRequest($"Not enough stock for product {product.Name}");

                var orderDetail = new OrderDetail
                {
                    ProductId = detailDto.ProductId,
                    Quantity = detailDto.Quantity,
                    UnitPrice = product.Price
                };

                order.OrderDetails.Add(orderDetail);
                totalAmount += orderDetail.TotalPrice;
            }

            order.TotalAmount = totalAmount;

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, await GetOrderDto(order));
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByUser(int userId)
        {
            var orders = await context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            var orderDtos = new List<OrderDto>();
            var user = await userService.GetUserByIdAsync(userId);

            foreach (var order in orders)
            {
                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    UserName = user?.Username ?? "Unknown",
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    OrderDetails = new List<OrderDetailDto>()
                };

                foreach (var detail in order.OrderDetails)
                {
                    var product = await productService.GetProductByIdAsync(detail.ProductId);
                    orderDto.OrderDetails.Add(new OrderDetailDto
                    {
                        Id = detail.Id,
                        ProductId = detail.ProductId,
                        ProductName = product?.Name ?? "Unknown",
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        TotalPrice = detail.TotalPrice
                    });
                }

                orderDtos.Add(orderDto);
            }

            return Ok(orderDtos);
        }

        private async Task<OrderDto> GetOrderDto(Order order)
        {
            var user = await userService.GetUserByIdAsync(order.UserId);
            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = user?.Username ?? "Unknown",
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                OrderDetails = new List<OrderDetailDto>()
            };

            foreach (var detail in order.OrderDetails)
            {
                var product = await productService.GetProductByIdAsync(detail.ProductId);
                orderDto.OrderDetails.Add(new OrderDetailDto
                {
                    Id = detail.Id,
                    ProductId = detail.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    TotalPrice = detail.TotalPrice
                });
            }

            return orderDto;
        }
    }
}
