using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
     public class GatewayController(IHttpClientFactory httpClientFactory) : ControllerBase
     {
         // User Service Routes
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] object loginData)
        {
            return await ProxyRequest("UserService", "/api/auth/login", HttpMethod.Post, loginData);
        }

        [HttpPost("auth/register")]
        public async Task<IActionResult> Register([FromBody] object registerData)
        {
            return await ProxyRequest("UserService", "/api/auth/register", HttpMethod.Post, registerData);
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            return await ProxyRequestWithAuth("UserService", "/api/users", HttpMethod.Get);
        }

        [HttpGet("users/{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            return await ProxyRequestWithAuth("UserService", $"/api/users/{id}", HttpMethod.Get);
        }

        [HttpDelete("users/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(int id)
        {
            return await ProxyRequestWithAuth("UserService", $"/api/users/{id}", HttpMethod.Delete);
        }

        // Product Service Routes
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            return await ProxyRequest("ProductService", "/api/products", HttpMethod.Get);
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            return await ProxyRequest("ProductService", $"/api/products/{id}", HttpMethod.Get);
        }

        [HttpPost("products")]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromBody] object productData)
        {
            return await ProxyRequestWithAuth("ProductService", "/api/products", HttpMethod.Post, productData);
        }

        [HttpPut("products/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] object productData)
        {
            return await ProxyRequestWithAuth("ProductService", $"/api/products/{id}", HttpMethod.Put, productData);
        }

        [HttpDelete("products/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            return await ProxyRequestWithAuth("ProductService", $"/api/products/{id}", HttpMethod.Delete);
        }

        // Order Service Routes
        [HttpGet("orders")]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            return await ProxyRequestWithAuth("OrderService", "/api/orders", HttpMethod.Get);
        }

        [HttpGet("orders/{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            return await ProxyRequestWithAuth("OrderService", $"/api/orders/{id}", HttpMethod.Get);
        }

        [HttpPost("orders")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] object orderData)
        {
            return await ProxyRequestWithAuth("OrderService", "/api/orders", HttpMethod.Post, orderData);
        }

        [HttpGet("orders/user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            return await ProxyRequestWithAuth("OrderService", $"/api/orders/user/{userId}", HttpMethod.Get);
        }

        private async Task<IActionResult> ProxyRequest(string serviceName, string path, HttpMethod method, object? data = null)
        {
            var client = httpClientFactory.CreateClient(serviceName);
            var request = new HttpRequestMessage(method, path);

            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = content,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { message = "Service unavailable", error = ex.Message });
            }
        }

        private async Task<IActionResult> ProxyRequestWithAuth(string serviceName, string path, HttpMethod method, object? data = null)
        {
            var client = httpClientFactory.CreateClient(serviceName);
            var request = new HttpRequestMessage(method, path);

            // Forward Authorization header
            if (Request.Headers.ContainsKey("Authorization"))
            {
                request.Headers.Add("Authorization", Request.Headers["Authorization"].ToString());
            }

            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = content,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { message = "Service unavailable", error = ex.Message });
            }
        }
    }
}
