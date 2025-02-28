using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MockBankAPI.Models;
using MockBankAPI.Controllers;


var builder = WebApplication.CreateBuilder(args);

// ======== Database Configuration ========
builder.Services.AddDbContext<MockBankDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MockBankDB");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new ArgumentNullException("MockBankDB", "Database connection string is required in configuration.");
    }
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// ======== Add Logging ========
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// ======== Add Services ========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mock Bank API", Version = "v1" });
});

// ========Add HTTP Client (no static config; dynamic in controller) ========
builder.Services.AddHttpClient();

var app = builder.Build();

// ======== Log Startup ========
app.Logger.LogInformation("Starting Mock Bank API...");

// ======== Configure Middleware Pipeline ========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ======== Read ServerUrl from configuration ========
var serverUrl = app.Configuration["ServerUrl"];
if (string.IsNullOrEmpty(serverUrl))
{
    app.Logger.LogWarning("ServerUrl missing from configuration. Defaulting to http://0.0.0.0:5000.");
    serverUrl = "http://0.0.0.0:5000";
}
app.Urls.Add(serverUrl);
app.Logger.LogInformation("API configured to listen on {ServerUrl}", serverUrl);

app.MapControllers();

app.Run();