using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthAPI.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;

        public ProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            return MapToUserDto(user);
        }

        public async Task<AuthResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth.Value;
                user.Age = CalculateAge(request.DateOfBirth.Value); // ✅ Now assigns to int?
            }

            if (!string.IsNullOrWhiteSpace(request.Gender))
                user.Gender = request.Gender; // ✅ Now assigns to string?

            if (request.MaxDistance.HasValue)
                user.MaxDistance = request.MaxDistance.Value; // ✅ Now assigns to int?

            if (!string.IsNullOrWhiteSpace(request.City))
                user.City = request.City;

            if (!string.IsNullOrWhiteSpace(request.State))
                user.State = request.State;

            if (!string.IsNullOrWhiteSpace(request.Address))
                user.Address = request.Address; // ✅ Added Address update

            if (!string.IsNullOrWhiteSpace(request.Country))
                user.Country = request.Country; // ✅ Added Country update

            if (!string.IsNullOrWhiteSpace(request.InterestedIn))
                user.InterestedIn = request.InterestedIn; // ✅ Added InterestedIn update

            if (request.PreferredAgeMin.HasValue)
                user.PreferredAgeMin = request.PreferredAgeMin.Value; // ✅ Added age preference

            if (request.PreferredAgeMax.HasValue)
                user.PreferredAgeMax = request.PreferredAgeMax.Value; // ✅ Added age preference

            if (request.Hobbies != null && request.Hobbies.Count > 0)
                user.Hobbies = JsonSerializer.Serialize(request.Hobbies);

            if (request.Interests != null && request.Interests.Count > 0)
                user.Interests = JsonSerializer.Serialize(request.Interests);

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

            if (request.Bio != null)
                user.Bio = request.Bio;

            if (request.Occupation != null)
                user.Occupation = request.Occupation;

            if (request.Education != null)
                user.Education = request.Education;

            if (request.Height.HasValue)
                user.Height = request.Height.Value;

            user.LastActive = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Profile updated successfully",
                User = MapToUserDto(user)
            };
        }

        public async Task<bool> DeleteAccountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Delete all user's photos
            var photos = await _context.Photos.Where(p => p.UserId == userId).ToListAsync();
            _context.Photos.RemoveRange(photos);

            // Delete all user's matches
            var matches = await _context.Matches
                .Where(m => m.UserId == userId || m.TargetUserId == userId)
                .ToListAsync();
            _context.Matches.RemoveRange(matches);

            // Delete user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
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
                PhoneNumber = string.Empty, // Not used anymore
                DateOfBirth = user.DateOfBirth ?? DateTime.MinValue, // ✅ Handle null
                Age = user.Age ?? 0, // ✅ Handle null
                Gender = user.Gender ?? string.Empty, // ✅ Handle null
                MaxDistance = user.MaxDistance ?? 0, // ✅ Handle null
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
    }
}