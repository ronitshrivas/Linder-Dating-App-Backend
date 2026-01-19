// ===== Program.cs =====
// Replace everything in Program.cs with this code

using AuthAPI.Data;
using AuthAPI.Services;
using Linder.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. ADD SERVICES TO CONTAINER =====

// Add Controllers
builder.Services.AddControllers();

// Add Swagger for API documentation and testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configure Swagger to support JWT authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// Add Database Context (using In-Memory database for demo)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("AuthDb"));

// For Production, use SQL Server instead:
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS (Cross-Origin Resource Sharing) - allows frontend to call API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== 2. CONFIGURE JWT AUTHENTICATION =====

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new Exception("JWT SecretKey not configured!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,              // Check who issued the token
        ValidateAudience = true,            // Check who can use the token
        ValidateLifetime = true,            // Check if token is expired
        ValidateIssuerSigningKey = true,   // Verify token signature
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// ===== 3. REGISTER CUSTOM SERVICES =====0

// Register AuthService (Scoped = new instance per request)
builder.Services.AddScoped<IAuthService, AuthService>();

// ===== 4. BUILD THE APP =====

var app = builder.Build();

// ===== 5. CONFIGURE HTTP REQUEST PIPELINE =====

// Enable Swagger in Development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Enable Authentication (must be before Authorization!)
app.UseAuthentication();

// Enable Authorization
app.UseAuthorization();

// Map Controllers to routes
app.MapControllers();

// ===== 6. RUN THE APPLICATION =====

app.Run();

/*
EXPLANATION OF THE PIPELINE:

1. AddControllers() - Enables API controllers
2. AddSwagger() - Adds API documentation UI
3. AddDbContext() - Configures database
4. AddCors() - Allows frontend to call API
5. AddAuthentication() - Configures JWT authentication
6. AddScoped<IAuthService>() - Registers our service

MIDDLEWARE ORDER (VERY IMPORTANT!):
1. UseSwagger() - Swagger UI
2. UseHttpsRedirection() - Redirect to HTTPS
3. UseCors() - Handle CORS
4. UseAuthentication() - Verify JWT tokens
5. UseAuthorization() - Check permissions
6. MapControllers() - Route to controllers

NEXT: How to run and test the API
*/