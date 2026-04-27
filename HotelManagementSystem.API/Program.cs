using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Repositories;
using HotelManagementSystem.Shared.Data;
using HotelManagementSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HotelManagementSystem.API.Middleware;
using HotelManagementSystem.API.Profiles;
using HotelManagementSystem.API.Services;
using HotelManagementSystem.API.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not found or empty.");
}

// Serilog — fully configured from appsettings.json (Serilog section).
// File sink writes to Logs/log-YYYYMMDD.txt (daily, 7-day retention).
// MSSqlServer sink resolves the "DefaultConnection" name from ConnectionStrings
// and auto-creates the dbo.Logs table on first write.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);
});

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<HotelDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// File storage — switched via appsettings.json "StorageProvider": "Local" | "AzureBlob"
var storageProvider = builder.Configuration["StorageProvider"] ?? "Local";
if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
else
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// CORS — allow the Angular dev server and production origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",   // Angular dev server
                "https://localhost:4200"   // Angular dev server (HTTPS)
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HotelManagementSystem API", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add AutoMapper services before building the app
builder.Services.AddAutoMapper(typeof(MappingProfile));


// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
    };
});

// Add authorization policies
builder.Services.AddAuthorizationBuilder()
                 .AddPolicy("ManageRooms",       policy => policy.RequireClaim("Permission", "ManageRooms"))
                 .AddPolicy("ManageRoles",       policy => policy.RequireClaim("Permission", "ManageRoles"))
                 .AddPolicy("ManageUsers",       policy => policy.RequireClaim("Permission", "ManageUsers"))
                 .AddPolicy("ManagePermissions", policy => policy.RequireClaim("Permission", "ManagePermissions"))
                 .AddPolicy("ManageRoomTypes",   policy => policy.RequireClaim("Permission", "ManageRoomTypes"))
                 .AddPolicy("ManageAmenities",   policy => policy.RequireClaim("Permission", "ManageAmenities"))
                 .AddPolicy("ManageStaff",       policy => policy.RequireClaim("Permission", "ManageStaff"))
                 .AddPolicy("ManageBookings",    policy => policy.RequireClaim("Permission", "ManageBookings"))
                 .AddPolicy("ViewReports",       policy => policy.RequireClaim("Permission", "ViewReports"));

var app = builder.Build();
// Configure the HTTP request pipeline.

// Global exception handler — must be early so it wraps all subsequent middleware.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Structured request logging via Serilog (method, path, status, elapsed-ms).
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelManagementSystem API v1"));
}

app.UseRouting();

// Serve wwwroot/uploads/ as /uploads/* (used by LocalFileStorageService)
app.UseStaticFiles();

app.UseCors("AngularClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "api/{controller=Account}/{action=Login}/{id?}");

app.Run();