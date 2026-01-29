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
    //public interface IAuthService
    //{
    //    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    //    Task<CompleteProfileResponse> CompleteProfileAsync(int userId, CompleteProfileRequest request);
    //    Task<ProfileStatusDto> GetProfileStatusAsync(int userId);
    //    Task<AuthResponse> LoginAsync(LoginRequest request);
    //}

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ===== STEP 1: BASIC REGISTRATION =====
        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Full name, email, and password are required"
                };
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user with ONLY basic info (all Step 2 fields are NULL)
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,

                // All Step 2 fields remain NULL
                DateOfBirth = null,
                Age = null,
                Gender = null,
                InterestedIn = null,
                MaxDistance = null,
                Address = null,
                City = null,
                State = null,
                Country = null,
                PreferredAgeMin = null,
                PreferredAgeMax = null,
                ProfilePhotos = "[]", // Empty array

                IsProfileComplete = false, // Profile NOT complete yet
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new RegisterResponse
            {
                Success = true,
                Message = "Registration successful! Please complete your profile.",
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                ProfileComplete = false
            };
        }

        // ===== STEP 2: COMPLETE PROFILE =====
        public async Task<CompleteProfileResponse> CompleteProfileAsync(int userId, CompleteProfileRequest request)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return new CompleteProfileResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            if (user.IsProfileComplete)
            {
                return new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Profile already completed"
                };
            }

            // Validate minimum photos (2 required)
            if (request.ProfilePhotos == null || request.ProfilePhotos.Count < 2)
            {
                return new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Please upload at least 2 photos (maximum 6)"
                };
            }

            // Validate maximum photos (6 max)
            if (request.ProfilePhotos.Count > 6)
            {
                return new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Maximum 6 photos allowed"
                };
            }

            // Validate age (must be 18+)
            var age = CalculateAge(request.DateOfBirth);
            if (age < 18)
            {
                return new CompleteProfileResponse
                {
                    Success = false,
                    Message = "You must be at least 18 years old"
                };
            }

            // Validate preferred age range
            if (request.PreferredAgeMin > request.PreferredAgeMax)
            {
                return new CompleteProfileResponse
                {
                    Success = false,
                    Message = "Minimum age cannot be greater than maximum age"
                };
            }

            // Handle "Prefer not to say" gender
            if (request.Gender == "Prefer not to say")
            {
                if (string.IsNullOrWhiteSpace(request.InterestedIn))
                {
                    return new CompleteProfileResponse
                    {
                        Success = false,
                        Message = "Please select who you're interested in (Male, Female, or Both)"
                    };
                }

                if (request.InterestedIn != "Male" &&
                    request.InterestedIn != "Female" &&
                    request.InterestedIn != "Both")
                {
                    return new CompleteProfileResponse
                    {
                        Success = false,
                        Message = "Interested in must be: Male, Female, or Both"
                    };
                }
            }

            // NOW set all the nullable fields from Step 2
            user.DateOfBirth = request.DateOfBirth; // Now has value
            user.Age = age; // Now has value
            user.Gender = request.Gender; // Now has value
            user.InterestedIn = request.InterestedIn;
            user.MaxDistance = request.MaxDistance; // Now has value
            user.Address = request.Address; // Now has value
            user.City = request.City;
            user.State = request.State;
            user.Country = request.Country;
            user.PreferredAgeMin = request.PreferredAgeMin; // Now has value
            user.PreferredAgeMax = request.PreferredAgeMax; // Now has value

            // Save photos
            user.ProfilePhotos = JsonSerializer.Serialize(request.ProfilePhotos);

            // Save optional fields
            user.Hobbies = JsonSerializer.Serialize(request.Hobbies ?? new List<string>());
            user.Interests = JsonSerializer.Serialize(request.Interests ?? new List<string>());
            user.ZodiacSign = request.ZodiacSign ?? string.Empty;
            user.SunSign = request.SunSign ?? string.Empty;
            user.MoonSign = request.MoonSign ?? string.Empty;
            user.RashiSign = request.RashiSign ?? string.Empty;
            user.Nakshatra = request.Nakshatra ?? string.Empty;
            user.ChineseZodiac = request.ChineseZodiac ?? string.Empty;
            user.Bio = request.Bio;
            user.Occupation = request.Occupation;
            user.Education = request.Education;
            user.Height = request.Height;

            // Mark profile as complete
            user.IsProfileComplete = true;
            user.LastActive = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CompleteProfileResponse
            {
                Success = true,
                Message = "Profile completed successfully!",
                User = MapToUserDto(user)
            };
        }

        // ===== GET PROFILE STATUS =====
        public async Task<ProfileStatusDto> GetProfileStatusAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return new ProfileStatusDto
                {
                    IsProfileComplete = false,
                    CurrentStep = "not_found",
                    MissingFields = new List<string> { "User not found" }
                };
            }

            var status = new ProfileStatusDto
            {
                IsProfileComplete = user.IsProfileComplete,
                CurrentStep = user.IsProfileComplete ? "completed" : "registered"
            };

            if (!user.IsProfileComplete)
            {
                var missingFields = new List<string>();

                if (string.IsNullOrEmpty(user.ProfilePhotos) || user.ProfilePhotos == "[]")
                    missingFields.Add("Profile photos (minimum 2)");

                if (!user.DateOfBirth.HasValue)
                    missingFields.Add("Date of birth");

                if (string.IsNullOrEmpty(user.Gender))
                    missingFields.Add("Gender");

                if (string.IsNullOrEmpty(user.Address))
                    missingFields.Add("Address");

                if (!user.PreferredAgeMin.HasValue || !user.PreferredAgeMax.HasValue)
                    missingFields.Add("Preferred age range");

                if (!user.MaxDistance.HasValue)
                    missingFields.Add("Maximum distance");

                status.MissingFields = missingFields;
            }

            return status;
        }

        // ===== LOGIN =====
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

            // Check if profile is complete
            var message = user.IsProfileComplete
                ? "Login successful"
                : "Login successful! Please complete your profile.";

            return new AuthResponse
            {
                Success = true,
                Message = message,
                Token = token,
                User = MapToUserDto(user)
            };
        }

        // ===== HELPER METHODS =====

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
                PhoneNumber = string.Empty, // Not used in new flow
                DateOfBirth = user.DateOfBirth ?? DateTime.MinValue, // Default if null
                Age = user.Age ?? 0, // Default if null
                Gender = user.Gender ?? string.Empty, // Default if null
                MaxDistance = user.MaxDistance ?? 0, // Default if null
                City = user.City,
                State = user.State,
                ProfilePhotos = JsonSerializer.Deserialize<List<string>>(user.ProfilePhotos) ?? new List<string>(),
                Hobbies = JsonSerializer.Deserialize<List<string>>(user.Hobbies) ?? new List<string>(),
                Interests = JsonSerializer.Deserialize<List<string>>(user.Interests) ?? new List<string>(),
                ZodiacSign = user.ZodiacSign ?? string.Empty,
                SunSign = user.SunSign ?? string.Empty,
                MoonSign = user.MoonSign ?? string.Empty,
                RashiSign = user.RashiSign ?? string.Empty,
                Nakshatra = user.Nakshatra ?? string.Empty,
                ChineseZodiac = user.ChineseZodiac ?? string.Empty,
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("ProfileComplete", user.IsProfileComplete.ToString())
            };

            // Add gender and age ONLY if profile is complete (and they have values)
            if (user.IsProfileComplete && !string.IsNullOrEmpty(user.Gender))
            {
                claims.Add(new Claim("Gender", user.Gender));
            }

            if (user.IsProfileComplete && user.Age.HasValue)
            {
                claims.Add(new Claim("Age", user.Age.Value.ToString()));
            }

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