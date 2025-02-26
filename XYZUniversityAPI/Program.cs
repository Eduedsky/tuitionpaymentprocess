using Microsoft.EntityFrameworkCore;
using XYZUniversityAPI.Models;
using Microsoft.AspNetCore.Mvc;
using XYZUniversityAPI.Controllers;
using XYZUniversityAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.Run();
