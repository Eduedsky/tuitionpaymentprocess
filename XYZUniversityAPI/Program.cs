using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using XYZUniversityAPI.Models;
using XYZUniversityAPI.Repositories;
using XYZUniversityAPI.Services;
var builder = WebApplication.CreateBuilder(args);

// ======== Database Configuration ========
builder.Services.AddDbContext<ApplicationDbContext>((services, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new ArgumentNullException("DefaultConnection", "Database connection string is required in configuration.");
    }
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// Add Logging
builder.Services.AddLogging();

// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});
builder.Services.AddHttpClient();

// Add Repositories and Services
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var apiKey = app.Configuration["ApiKey"];

if (string.IsNullOrEmpty(apiKey))
{
    app.Logger.LogError("API key is missing from configuration. Please set 'Authentication:ApiKey'.");
    return;
}
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-API-Key", out var requestApiKey) ||
        string.IsNullOrEmpty(requestApiKey))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Api key is required" });
        return;
    }
    if (requestApiKey != apiKey)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
        return;
    }
    await next();
});
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
