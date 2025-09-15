using System.Text;
using System.Text.Json;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
builder.Services.AddHttpClient("UserService",
    client => { client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]); });

builder.Services.AddHttpClient("ProductService",
    client => { client.BaseAddress = new Uri(builder.Configuration["Services:ProductService"]); });

builder.Services.AddHttpClient("OrderService",
    client => { client.BaseAddress = new Uri(builder.Configuration["Services:OrderService"]); });

builder.Services.AddControllers();

// HealthChecks
builder.Services
    .AddHealthChecks()
    // Self-check (жив ли сам сервис)
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "self", "live" })

    // Зависимости ApiGateway — уникальные имена и теги
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:UserService"] + "/health/ready"),
        name: "UserService",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "deps", "ready" }
    )
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:ProductService"] + "/health/ready"),
        name: "ProductService",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "deps", "ready" }
    )
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:OrderService"] + "/health/ready"),
        name: "OrderService",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "deps", "ready" }
    );


builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.Zero; // без задержки перед первой публикацией
    options.Period = TimeSpan.FromSeconds(5); // обновление каждые 5 секунд
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

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

// /health/live — только self
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("self"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// /health/ready — self + все зависимости
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});


app.Run();