using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Repositories;
using HotelManagementSystem.Shared.Data;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.API.Profiles;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not found or empty.");
}

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AutoMapper services before building the app
builder.Services.AddAutoMapper(typeof(MappingProfile));

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();