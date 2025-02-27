using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "XYZ University API", Version = "v1" });
});
builder.Services.AddHttpClient();

// Add Repositories and Services
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

// Log Startup
app.Logger.LogInformation("Starting XYZ University API...");

// Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var apiKey = app.Configuration["Authentication:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    app.Logger.LogError("API key is missing from configuration. Please set 'Authentication:ApiKey'.");
    throw new InvalidOperationException("API key configuration missing.");
}

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-API-Key", out var requestApiKey) || string.IsNullOrEmpty(requestApiKey))
    {
        app.Logger.LogWarning("API key missing from request headers.");
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
        return;
    }
    if (requestApiKey != apiKey)
    {
        app.Logger.LogWarning("Invalid API key provided: {RequestApiKey}", requestApiKey);
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
        return;
    }
    await next(context);
});

// Read ServerUrl from configuration and set the URL binding
var serverUrl = app.Configuration["ASPNETCORE_URLS:ServerUrl"];
if (string.IsNullOrEmpty(serverUrl))
{
    app.Logger.LogError("ServerUrl is missing from configuration. Defaulting to http://0.0.0.0:5251.");
    serverUrl = "http://0.0.0.0:5251"; // Fallback default
}
app.Urls.Add(serverUrl);
app.Logger.LogInformation("API configured to listen on {ServerUrl}", serverUrl);


app.UseHttpsRedirection();

app.MapControllers();

app.Run();