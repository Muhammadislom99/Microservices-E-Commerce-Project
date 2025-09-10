using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyOpenTelemetry("ApiGatewayService");


// JWT Configuration
var jwtKey = "YourSuperSecretKey1234567890123456";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// HTTP Clients for microservices
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]);
});

builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ProductService"]);
});

builder.Services.AddHttpClient("OrderService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrderService"]);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});


// ... существующие сервисы (routing, OTel и т.д.)
var cfg = builder.Configuration;


// Привязка к readiness downstream сервисов (если нужно строго контролировать готовность шлюза)
var productReady = cfg["Dependencies:ProductServiceReadyUrl"];
var orderReady = cfg["Dependencies:OrderServiceReadyUrl"];
var userReady = cfg["Dependencies:UserServiceReadyUrl"];


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();