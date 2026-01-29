using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthAPI.Services
{
    public interface IModerationService
    {
        Task<bool> ReportUserAsync(int reporterId, ReportUserRequest request);
        Task<bool> BlockUserAsync(int blockerId, int blockedUserId);
        Task<bool> UnblockUserAsync(int blockerId, int blockedUserId);
        Task<List<UserDto>> GetBlockedUsersAsync(int userId);
    }

    public class ModerationService : IModerationService
    {
        private readonly AppDbContext _context;

        public ModerationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ReportUserAsync(int reporterId, ReportUserRequest request)
        {
            // Check if user exists
            var reportedUser = await _context.Users.FindAsync(request.ReportedUserId);
            if (reportedUser == null)
                return false;

            // Check if already reported
            var existingReport = await _context.UserReports
                .FirstOrDefaultAsync(r => r.ReporterId == reporterId &&
                                         r.ReportedUserId == request.ReportedUserId);

            if (existingReport != null)
                return false;

            var report = new UserReport
            {
                ReporterId = reporterId,
                ReportedUserId = request.ReportedUserId,
                Reason = request.Reason,
                Description = request.Description,
                ReportedAt = DateTime.UtcNow,
                IsResolved = false
            };

            _context.UserReports.Add(report);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> BlockUserAsync(int blockerId, int blockedUserId)
        {
            // Check if already blocked
            var existingBlock = await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == blockerId &&
                                         b.BlockedUserId == blockedUserId);

            if (existingBlock != null)
                return false;

            var block = new UserBlock
            {
                BlockerId = blockerId,
                BlockedUserId = blockedUserId,
                BlockedAt = DateTime.UtcNow
            };

            _context.UserBlocks.Add(block);

            // Remove any matches between these users
            var matches = await _context.Matches
                .Where(m => (m.UserId == blockerId && m.TargetUserId == blockedUserId) ||
                           (m.UserId == blockedUserId && m.TargetUserId == blockerId))
                .ToListAsync();

            _context.Matches.RemoveRange(matches);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnblockUserAsync(int blockerId, int blockedUserId)
        {
            var block = await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == blockerId &&
                                         b.BlockedUserId == blockedUserId);

            if (block == null)
                return false;

            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<UserDto>> GetBlockedUsersAsync(int userId)
        {
            var blockedUserIds = await _context.UserBlocks
                .Where(b => b.BlockerId == userId)
                .Select(b => b.BlockedUserId)
                .ToListAsync();

            var blockedUsers = await _context.Users
                .Where(u => blockedUserIds.Contains(u.Id))
                .ToListAsync();

            return blockedUsers.Select(MapToUserDto).ToList();
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
               // PhoneNumber = user.PhoneNumber,
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
    }
}