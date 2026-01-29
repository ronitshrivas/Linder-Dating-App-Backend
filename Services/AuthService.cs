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

            // Create user with ONLY basic info
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                IsProfileComplete = false,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new RegisterResponse
            {
                Success = true,
                Message = "Registration successful! You can complete your profile now or skip to browse.",
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                ProfileComplete = false
            };
        }

        // ===== STEP 2: COMPLETE PROFILE (ALL OPTIONAL) =====
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

            // Validate photos IF provided
            if (request.ProfilePhotos != null && request.ProfilePhotos.Count > 0)
            {
                if (request.ProfilePhotos.Count < 2)
                {
                    return new CompleteProfileResponse
                    {
                        Success = false,
                        Message = "If uploading photos, please provide at least 2 (maximum 6)"
                    };
                }

                if (request.ProfilePhotos.Count > 6)
                {
                    return new CompleteProfileResponse
                    {
                        Success = false,
                        Message = "Maximum 6 photos allowed"
                    };
                }

                user.ProfilePhotos = JsonSerializer.Serialize(request.ProfilePhotos);
            }

            // Update DateOfBirth and Age IF provided
            if (request.DateOfBirth.HasValue)
            {
                var age = CalculateAge(request.DateOfBirth.Value);

                if (age < 18)
                {
                    return new CompleteProfileResponse
                    {
                        Success = false,
                        Message = "You must be at least 18 years old"
                    };
                }

                user.DateOfBirth = request.DateOfBirth.Value;
                user.Age = age;
            }

            // Update Gender IF provided
            if (!string.IsNullOrWhiteSpace(request.Gender))
            {
                user.Gender = request.Gender;

                // Handle "Prefer not to say" - validate InterestedIn IF Gender is provided
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
            }

            // Update InterestedIn IF provided
            if (!string.IsNullOrWhiteSpace(request.InterestedIn))
            {
                user.InterestedIn = request.InterestedIn;
            }

            // Validate age range IF both provided
            if (request.PreferredAgeMin.HasValue && request.PreferredAgeMax.HasValue)
            {
                if (request.PreferredAgeMin > request.PreferredAgeMax)
                {
                    return new CompleteProfileResponse
                    {
                        Success = false,
                        Message = "Minimum age cannot be greater than maximum age"
                    };
                }

                user.PreferredAgeMin = request.PreferredAgeMin;
                user.PreferredAgeMax = request.PreferredAgeMax;
            }
            else if (request.PreferredAgeMin.HasValue)
            {
                user.PreferredAgeMin = request.PreferredAgeMin;
            }
            else if (request.PreferredAgeMax.HasValue)
            {
                user.PreferredAgeMax = request.PreferredAgeMax;
            }

            // Update MaxDistance IF provided
            if (request.MaxDistance.HasValue)
            {
                user.MaxDistance = request.MaxDistance;
            }

            // Update Address fields IF provided
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                user.Address = request.Address;
            }

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                user.City = request.City;
            }

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                user.State = request.State;
            }

            if (!string.IsNullOrWhiteSpace(request.Country))
            {
                user.Country = request.Country;
            }

            // Update Hobbies IF provided
            if (request.Hobbies != null && request.Hobbies.Count > 0)
            {
                user.Hobbies = JsonSerializer.Serialize(request.Hobbies);
            }

            // Update Interests IF provided
            if (request.Interests != null && request.Interests.Count > 0)
            {
                user.Interests = JsonSerializer.Serialize(request.Interests);
            }

            // Update Zodiac fields IF provided
            if (!string.IsNullOrWhiteSpace(request.ZodiacSign))
                user.ZodiacSign = request.ZodiacSign;

            if (!string.IsNullOrWhiteSpace(request.SunSign))
                user.SunSign = request.SunSign;

            if (!string.IsNullOrWhiteSpace(request.MoonSign))
                user.MoonSign = request.MoonSign;

            if (!string.IsNullOrWhiteSpace(request.RashiSign))
                user.RashiSign = request.RashiSign;

            if (!string.IsNullOrWhiteSpace(request.Nakshatra))
                user.Nakshatra = request.Nakshatra;

            if (!string.IsNullOrWhiteSpace(request.ChineseZodiac))
                user.ChineseZodiac = request.ChineseZodiac;

            // Update additional info IF provided
            if (!string.IsNullOrWhiteSpace(request.Bio))
                user.Bio = request.Bio;

            if (!string.IsNullOrWhiteSpace(request.Occupation))
                user.Occupation = request.Occupation;

            if (!string.IsNullOrWhiteSpace(request.Education))
                user.Education = request.Education;

            if (request.Height.HasValue)
                user.Height = request.Height;

            // Calculate profile completion percentage
            var completionPercentage = CalculateProfileCompletion(user);

            // Mark profile as complete if they filled enough fields (e.g., 50% or more)
            user.IsProfileComplete = completionPercentage >= 50;
            user.LastActive = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CompleteProfileResponse
            {
                Success = true,
                Message = user.IsProfileComplete
                    ? $"Profile updated successfully! ({completionPercentage}% complete)"
                    : $"Profile updated! Fill more details to increase your match rate. ({completionPercentage}% complete)",
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
                    MissingFields = new List<string> { "User not found" },
                    CompletionPercentage = 0
                };
            }

            var missingFields = new List<string>();
            var completionPercentage = CalculateProfileCompletion(user);

            // List optional but recommended fields that are missing
            if (string.IsNullOrEmpty(user.ProfilePhotos) || user.ProfilePhotos == "[]")
                missingFields.Add("Profile photos (recommended: 2-6)");

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

            if (string.IsNullOrEmpty(user.Bio))
                missingFields.Add("Bio");

            if (user.Hobbies == "[]" || string.IsNullOrEmpty(user.Hobbies))
                missingFields.Add("Hobbies");

            if (user.Interests == "[]" || string.IsNullOrEmpty(user.Interests))
                missingFields.Add("Interests");

            return new ProfileStatusDto
            {
                IsProfileComplete = user.IsProfileComplete,
                CurrentStep = user.IsProfileComplete ? "completed" : "registered",
                MissingFields = missingFields,
                CompletionPercentage = completionPercentage
            };
        }

        // Calculate profile completion percentage
        private int CalculateProfileCompletion(User user)
        {
            int totalFields = 15; // Total important fields
            int filledFields = 0;

            // Check each important field
            if (!string.IsNullOrEmpty(user.ProfilePhotos) && user.ProfilePhotos != "[]")
                filledFields += 2; // Photos are worth 2 points

            if (user.DateOfBirth.HasValue) filledFields++;
            if (!string.IsNullOrEmpty(user.Gender)) filledFields++;
            if (!string.IsNullOrEmpty(user.Address)) filledFields++;
            if (user.PreferredAgeMin.HasValue) filledFields++;
            if (user.PreferredAgeMax.HasValue) filledFields++;
            if (user.MaxDistance.HasValue) filledFields++;
            if (!string.IsNullOrEmpty(user.Bio)) filledFields++;

            if (user.Hobbies != "[]" && !string.IsNullOrEmpty(user.Hobbies))
                filledFields++;

            if (user.Interests != "[]" && !string.IsNullOrEmpty(user.Interests))
                filledFields++;

            if (!string.IsNullOrEmpty(user.Occupation)) filledFields++;
            if (!string.IsNullOrEmpty(user.Education)) filledFields++;
            if (user.Height.HasValue) filledFields++;
            if (!string.IsNullOrEmpty(user.ZodiacSign)) filledFields++;

            return (int)((double)filledFields / totalFields * 100);
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

            user.LastActive = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var completionPercentage = CalculateProfileCompletion(user);

            var message = user.IsProfileComplete
                ? "Login successful"
                : $"Login successful! Complete your profile to get better matches. ({completionPercentage}% complete)";

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
                PhoneNumber = string.Empty,
                DateOfBirth = user.DateOfBirth ?? DateTime.MinValue,
                Age = user.Age ?? 0,
                Gender = user.Gender ?? string.Empty,
                MaxDistance = user.MaxDistance ?? 0,
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