using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AuthAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> SignupAsync(SignupRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email, password, and full name are required"
                };
            }

            // Validate age (must be 18+)
            var age = CalculateAge(request.DateOfBirth);
            if (age < 18)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "You must be at least 18 years old to register"
                };
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            // Validate minimum 6 photos
            if (request.ProfilePhotos == null || request.ProfilePhotos.Count < 6)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Please upload at least 6 profile photos"
                };
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Age = age,
                Gender = request.Gender,
                MaxDistance = request.MaxDistance,
                City = request.City,
                State = request.State,

                // Convert lists to JSON strings for storage
                ProfilePhotos = JsonSerializer.Serialize(request.ProfilePhotos),
                Hobbies = JsonSerializer.Serialize(request.Hobbies),
                Interests = JsonSerializer.Serialize(request.Interests),

                // Horoscope details
                ZodiacSign = request.ZodiacSign ?? string.Empty,
                SunSign = request.SunSign ?? string.Empty,
                MoonSign = request.MoonSign ?? string.Empty,
                RashiSign = request.RashiSign ?? string.Empty,
                Nakshatra = request.Nakshatra ?? string.Empty,
                ChineseZodiac = request.ChineseZodiac ?? string.Empty,

                // Optional details
                Bio = request.Bio,
                Occupation = request.Occupation,
                Education = request.Education,
                Height = request.Height,

                IsProfileComplete = true,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "User registered successfully",
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Update last active
            user.LastActive = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = MapToUserDto(user)
            };
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Age = user.Age,
                Gender = user.Gender,
                MaxDistance = user.MaxDistance,
                City = user.City,
                State = user.State,

                // Deserialize JSON strings back to lists
                ProfilePhotos = JsonSerializer.Deserialize<List<string>>(user.ProfilePhotos) ?? new List<string>(),
                Hobbies = JsonSerializer.Deserialize<List<string>>(user.Hobbies) ?? new List<string>(),
                Interests = JsonSerializer.Deserialize<List<string>>(user.Interests) ?? new List<string>(),

                ZodiacSign = user.ZodiacSign,
                SunSign = user.SunSign,
                MoonSign = user.MoonSign,
                RashiSign = user.RashiSign,
                Nakshatra = user.Nakshatra,
                ChineseZodiac = user.ChineseZodiac,

                Bio = user.Bio,
                Occupation = user.Occupation,
                Education = user.Education,
                Height = user.Height,

                IsProfileComplete = user.IsProfileComplete,
                CreatedAt = user.CreatedAt
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyMinimum32CharactersLong!123";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Gender", user.Gender),
                new Claim("Age", user.Age.ToString())
            };

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "LinderAPI",
                audience: jwtSettings["Audience"] ?? "LinderUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}