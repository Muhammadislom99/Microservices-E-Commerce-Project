using System.Text;
using System.Text.Json;
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
builder.Services.AddHttpClient("UserService",
    client => { client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]); });

builder.Services.AddHttpClient("ProductService",
    client => { client.BaseAddress = new Uri(builder.Configuration["Services:ProductService"]); });

builder.Services.AddHttpClient("OrderService",
    client => { client.BaseAddress = new Uri(builder.Configuration["Services:OrderService"]); });

builder.Services.AddControllers();

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri(builder.Configuration["Services:OrderService"] + "/health"), name: "UserService",
        tags: ["all", "service"])
    .AddUrlGroup(new Uri(builder.Configuration["Services:ProductService"] + "/health"), name: "ProductService",
        tags: ["all", "service"])
    .AddUrlGroup(new Uri(builder.Configuration["Services:OrderService"] + "/health"), name: "OrderService",
        tags: ["all", "services"]);


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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});


app.Run();