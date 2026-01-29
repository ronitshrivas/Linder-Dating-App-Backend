//using AuthAPI.Data;
//using AuthAPI.Hubs;
//using AuthAPI.Services;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//// ===== 1. SERVICES =====
//builder.Services.AddControllers();
//builder.Services.AddSignalR();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(options =>
//{
//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "JWT Authorization header using the Bearer scheme."
//    });
//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            new string[] {}
//        }
//    });
//});

//// ===== 2. DATABASE =====
//// Get connection string from appsettings.json
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//Console.WriteLine("✅ Configuring SQL Server database");
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(connectionString));

//// ===== 3. CORS =====
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyMethod()
//              .AllowAnyHeader();
//    });
//});

//// ===== 4. JWT =====
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyMinimum32CharactersLong!123";

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"] ?? "LinderAPI",
//        ValidAudience = jwtSettings["Audience"] ?? "LinderUsers",
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
//    };

//    // SignalR JWT authentication
//    options.Events = new JwtBearerEvents
//    {
//        OnMessageReceived = context =>
//        {
//            var accessToken = context.Request.Query["access_token"];
//            var path = context.HttpContext.Request.Path;

//            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
//            {
//                context.Token = accessToken;
//            }

//            return Task.CompletedTask;
//        }
//    };
//});

//// ===== 5. REGISTER SERVICES =====
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IPhotoService, PhotoService>();
//builder.Services.AddScoped<IProfileService, ProfileService>();
//builder.Services.AddScoped<IMatchingService, MatchingService>();
//builder.Services.AddScoped<IMessageService, MessageService>();
//builder.Services.AddScoped<IModerationService, ModerationService>();

//var app = builder.Build();

//// ===== 6. AUTO-CREATE DATABASE =====
//using (var scope = app.Services.CreateScope())
//{
//    try
//    {
//        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        Console.WriteLine("🔧 Checking database connection...");

//        // This will automatically create database and tables on first run
//        context.Database.Migrate();

//        Console.WriteLine("✅ Database ready!");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"❌ Database error: {ex.Message}");
//        Console.WriteLine($"Stack trace: {ex.StackTrace}");
//    }
//}

//// ===== 7. MIDDLEWARE =====
//app.UseSwagger();
//app.UseSwaggerUI();

//// Don't redirect to HTTPS on MonsterASP (they handle SSL)
//// app.UseHttpsRedirection();

//app.UseCors("AllowAll");
//app.UseAuthentication();
//app.UseAuthorization();
//app.MapControllers();
//app.MapHub<ChatHub>("/chathub");

//// Health check endpoint
//app.MapGet("/", () => Results.Ok(new 
//{ 
//    status = "healthy",
//    api = "Linder Dating API",
//    version = "1.0",
//    timestamp = DateTime.UtcNow 
//}));

//app.MapGet("/health", () => Results.Ok(new 
//{ 
//    status = "healthy",
//    database = "connected",
//    timestamp = DateTime.UtcNow 
//}));

//Console.WriteLine("🚀 Linder Dating API is starting...");
//Console.WriteLine($"📚 Swagger UI: /swagger");
//Console.WriteLine($"❤️  Health Check: /health");

//app.Run();




using AuthAPI.Data;
using AuthAPI.Hubs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. ADD SERVICES TO CONTAINER =====

builder.Services.AddControllers();

// Add SignalR for real-time messaging
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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

// Add Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("LinderDb"));

// For Production SQL Server:
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React/Vue frontends
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

// ===== 2. CONFIGURE JWT AUTHENTICATION =====

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyMinimum32CharactersLong!123";

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
        ValidIssuer = jwtSettings["Issuer"] ?? "LinderAPI",
        ValidAudience = jwtSettings["Audience"] ?? "LinderUsers",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // SignalR JWT authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// ===== 3. REGISTER CUSTOM SERVICES =====

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IModerationService, ModerationService>();

// ===== 4. BUILD THE APP =====

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5264";
app.Urls.Add($"http://0.0.0.0:{port}");

// ===== 5. CONFIGURE HTTP REQUEST PIPELINE =====

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/chathub");

// ===== 6. RUN THE APPLICATION =====

app.Run();














/*
 * 
 * using AuthAPI.Data;
using AuthAPI.Hubs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. ADD SERVICES =====

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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

// ===== 2. DATABASE CONFIGURATION (AUTO-CREATE) =====

// Choose your database provider (uncomment ONE option):

// OPTION 1: SQLite (Recommended for Development) ✅
// - No installation needed
// - Creates Linder.db file automatically
// - Perfect for getting started
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Linder.db"));

// OPTION 2: SQL Server (For Production/Windows)
// Requires SQL Server installed
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// OPTION 3: PostgreSQL (For Production/Cloud)
// Requires PostgreSQL installed
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// OPTION 4: In-Memory (Testing Only - Data lost on restart)
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseInMemoryDatabase("LinderDb"));

// ===== 3. CORS =====

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:8080")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===== 4. JWT AUTHENTICATION =====

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyMinimum32CharactersLong!123";

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
        ValidIssuer = jwtSettings["Issuer"] ?? "LinderAPI",
        ValidAudience = jwtSettings["Audience"] ?? "LinderUsers",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // SignalR JWT authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

// ===== 5. REGISTER SERVICES =====

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IModerationService, ModerationService>();

// ===== 6. BUILD APP =====

var app = builder.Build();

// ===== 7. AUTO-CREATE DATABASE & SEED DATA =====

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Apply any pending migrations and create database
        Console.WriteLine("🔧 Checking database...");
        context.Database.EnsureCreated();
        
        // Optional: Apply migrations (for SQL Server/PostgreSQL)
        // context.Database.Migrate();
        
        Console.WriteLine("✅ Database ready!");
        
        // Optional: Seed test data (uncomment if needed)
        // await SeedTestData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred creating the database.");
    }
}

// ===== 8. CONFIGURE PIPELINE =====

var port = Environment.GetEnvironmentVariable("PORT") ?? "5264";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    database = "connected"
}));

Console.WriteLine("🚀 Linder Dating API is running!");
Console.WriteLine($"📍 API: http://localhost:{port}");
Console.WriteLine($"📚 Swagger: http://localhost:{port}/swagger");

app.Run();

// ===== 9. OPTIONAL: SEED TEST DATA =====

async Task SeedTestData(AppDbContext context)
{
    if (context.Users.Any())
    {
        Console.WriteLine("ℹ️  Database already has data, skipping seed.");
        return;
    }

    Console.WriteLine("🌱 Seeding test data...");

    // Create test users
    var users = new[]
    {
        new AuthAPI.Models.User
        {
            FullName = "Alice Johnson",
            Email = "alice@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1995, 5, 15),
            Age = 28,
            Gender = "Female",
            MaxDistance = 50,
            City = "New York",
            State = "NY",
            ProfilePhotos = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                "https://picsum.photos/400/400?random=1",
                "https://picsum.photos/400/400?random=2",
                "https://picsum.photos/400/400?random=3",
                "https://picsum.photos/400/400?random=4",
                "https://picsum.photos/400/400?random=5",
                "https://picsum.photos/400/400?random=6"
            }),
            Hobbies = System.Text.Json.JsonSerializer.Serialize(new[] { "Reading", "Traveling", "Yoga" }),
            Interests = System.Text.Json.JsonSerializer.Serialize(new[] { "Technology", "Art", "Food & Dining" }),
            ZodiacSign = "Taurus",
            SunSign = "Taurus",
            RashiSign = "Vrishabha (Taurus)",
            Nakshatra = "Rohini",
            ChineseZodiac = "Pig",
            Bio = "Love to travel and meet new people!",
            Occupation = "Software Engineer",
            Education = "Bachelor's in CS",
            Height = 165,
            IsProfileComplete = true,
            CreatedAt = DateTime.UtcNow,
            LastActive = DateTime.UtcNow
        },
        new AuthAPI.Models.User
        {
            FullName = "Bob Smith",
            Email = "bob@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            PhoneNumber = "+1234567891",
            DateOfBirth = new DateTime(1993, 8, 20),
            Age = 30,
            Gender = "Male",
            MaxDistance = 50,
            City = "New York",
            State = "NY",
            ProfilePhotos = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                "https://picsum.photos/400/400?random=7",
                "https://picsum.photos/400/400?random=8",
                "https://picsum.photos/400/400?random=9",
                "https://picsum.photos/400/400?random=10",
                "https://picsum.photos/400/400?random=11",
                "https://picsum.photos/400/400?random=12"
            }),
            Hobbies = System.Text.Json.JsonSerializer.Serialize(new[] { "Reading", "Gaming", "Cooking" }),
            Interests = System.Text.Json.JsonSerializer.Serialize(new[] { "Technology", "Science", "Food & Dining" }),
            ZodiacSign = "Leo",
            SunSign = "Leo",
            RashiSign = "Simha (Leo)",
            Nakshatra = "Magha",
            ChineseZodiac = "Rooster",
            Bio = "Tech enthusiast and foodie!",
            Occupation = "Product Manager",
            Education = "MBA",
            Height = 180,
            IsProfileComplete = true,
            CreatedAt = DateTime.UtcNow,
            LastActive = DateTime.UtcNow
        }
    };

    context.Users.AddRange(users);
    await context.SaveChangesAsync();

    Console.WriteLine("✅ Seeded 2 test users:");
    Console.WriteLine("   📧 alice@test.com / password123");
    Console.WriteLine("   📧 bob@test.com / password123");
}

/*
======================== SETUP INSTRUCTIONS ========================

1. INSTALL REQUIRED PACKAGE (Choose ONE):

   For SQLite (Recommended - Easiest):
   > dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 7.0.20

   For SQL Server:
   > dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 7.0.20

   For PostgreSQL:
   > dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 7.0.20

2. INSTALL OTHER PACKAGES:
   > dotnet add package Microsoft.EntityFrameworkCore --version 7.0.20
   > dotnet add package BCrypt.Net-Next --version 4.0.3
   > dotnet add package Microsoft.AspNetCore.SignalR --version 1.1.0
   > dotnet add package Swashbuckle.AspNetCore --version 6.5.0

3. RUN THE APP:
   > dotnet restore
   > dotnet run

   That's it! Database will be created automatically! ✅

======================== WHAT HAPPENS ON FIRST RUN ========================

✅ Database is created automatically (Linder.db for SQLite)
✅ All tables are created (Users, Photos, Matches, Messages, etc.)
✅ Test data is seeded (2 users: alice@test.com, bob@test.com)
✅ API starts at http://localhost:5264
✅ Swagger UI available at http://localhost:5264/swagger

======================== NO MANUAL SETUP NEEDED! ========================

Just run: dotnet run

Database location:
- SQLite: Linder.db file in project folder
- SQL Server: LinderDB database in SQL Server
- PostgreSQL: LinderDB database in PostgreSQL

====================================================================
*/

