using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AuthAPI.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<AdvancedAnalytics> GetAdvancedAnalyticsAsync();

        // User Management
        Task<PaginatedResult<AdminUserDto>> GetAllUsersAsync(int page, int pageSize, string? search, bool? isBanned);
        Task<AdminUserDetailDto?> GetUserDetailAsync(int userId);
        Task<AdminResponse> BanUserAsync(int adminId, BanUserRequest request);
        Task<AdminResponse> UnbanUserAsync(int adminId, int userId);
        Task<AdminResponse> DeleteUserAsync(int adminId, int userId);
        Task<AdminResponse> MakeAdminAsync(int adminId, int userId);
        Task<AdminResponse> RemoveAdminAsync(int adminId, int userId);

        // Bulk Actions
        Task<BulkActionResult> BulkBanUsersAsync(int adminId, BulkBanRequest request);
        Task<BulkActionResult> BulkDeleteUsersAsync(int adminId, BulkDeleteRequest request);

        // Reports
        Task<PaginatedResult<AdminReportDto>> GetReportsAsync(int page, int pageSize, bool? isResolved);
        Task<AdminResponse> ResolveReportAsync(int adminId, ResolveReportRequest request);

        // Content
        Task<AdminResponse> DeleteMessageAsync(int adminId, int messageId);
        Task<AdminResponse> DeletePhotoAsync(int adminId, int photoId);
        Task<PaginatedResult<AdminMessageDto>> GetAllMessagesAsync(int page, int pageSize, int? userId);

        // Activity Log
        Task<PaginatedResult<AdminActivityLog>> GetActivityLogsAsync(int page, int pageSize, int? adminId);

        // App Settings
        Task<List<AppSetting>> GetAllSettingsAsync();
        Task<AdminResponse> UpdateSettingsAsync(int adminId, UpdateSettingsRequest request);

        // Export
        Task<byte[]> ExportUsersToCsvAsync();
    }

    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        // ===== LOG ADMIN ACTION =====
        private async Task LogActionAsync(int adminId, string action, string targetType, int? targetId = null, string? targetName = null, string? details = null)
        {
            var log = new AdminLog
            {
                AdminId = adminId,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                Details = details,
                PerformedAt = DateTime.UtcNow
            };
            _context.AdminLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // ===== DASHBOARD STATS =====
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-7);

            var last7Days = new List<DailyStats>();
            for (int i = 6; i >= 0; i--)
            {
                var date = now.Date.AddDays(-i);
                var nextDate = date.AddDays(1);
                last7Days.Add(new DailyStats
                {
                    Date = date.ToString("MMM dd"),
                    NewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate),
                    NewMatches = await _context.Matches.CountAsync(m => m.IsMatch && m.MatchedAt >= date && m.MatchedAt < nextDate),
                    NewMessages = await _context.Messages.CountAsync(m => !m.IsDeleted && m.SentAt >= date && m.SentAt < nextDate)
                });
            }

            return new DashboardStats
            {
                TotalUsers = await _context.Users.CountAsync(),
                NewUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= todayStart),
                NewUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekStart),
                ActiveUsersToday = await _context.Users.CountAsync(u => u.LastActive >= todayStart),
                TotalMatches = await _context.Matches.CountAsync(m => m.IsMatch),
                TotalMessages = await _context.Messages.CountAsync(m => !m.IsDeleted),
                PendingReports = await _context.UserReports.CountAsync(r => !r.IsResolved),
                ResolvedReports = await _context.UserReports.CountAsync(r => r.IsResolved),
                BannedUsers = await _context.Users.CountAsync(u => u.IsBanned),
                ProfilesComplete = await _context.Users.CountAsync(u => u.IsProfileComplete),
                Last7Days = last7Days
            };
        }

        // ===== ADVANCED ANALYTICS =====
        public async Task<AdvancedAnalytics> GetAdvancedAnalyticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();

            // Gender Distribution
            var genderGroups = await _context.Users
                .Where(u => u.Gender != null)
                .GroupBy(u => u.Gender)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToListAsync();

            var genderDist = genderGroups.Select(g => new DistributionItem
            {
                Label = g.Label ?? "Unknown",
                Count = g.Count,
                Percentage = totalUsers > 0 ? Math.Round((double)g.Count / totalUsers * 100, 1) : 0
            }).ToList();

            // Age Distribution
            var ageDist = new List<DistributionItem>
            {
                new() { Label = "18-24", Count = await _context.Users.CountAsync(u => u.Age >= 18 && u.Age <= 24) },
                new() { Label = "25-34", Count = await _context.Users.CountAsync(u => u.Age >= 25 && u.Age <= 34) },
                new() { Label = "35-44", Count = await _context.Users.CountAsync(u => u.Age >= 35 && u.Age <= 44) },
                new() { Label = "45-54", Count = await _context.Users.CountAsync(u => u.Age >= 45 && u.Age <= 54) },
                new() { Label = "55+", Count = await _context.Users.CountAsync(u => u.Age >= 55) },
            };
            ageDist.ForEach(a => a.Percentage = totalUsers > 0 ? Math.Round((double)a.Count / totalUsers * 100, 1) : 0);

            // Country Distribution (top 10)
            var countryGroups = await _context.Users
                .Where(u => u.Country != null)
                .GroupBy(u => u.Country)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var countryDist = countryGroups.Select(g => new DistributionItem
            {
                Label = g.Label ?? "Unknown",
                Count = g.Count,
                Percentage = totalUsers > 0 ? Math.Round((double)g.Count / totalUsers * 100, 1) : 0
            }).ToList();

            // City Distribution (top 10)
            var cityGroups = await _context.Users
                .Where(u => u.City != null)
                .GroupBy(u => u.City)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var cityDist = cityGroups.Select(g => new DistributionItem
            {
                Label = g.Label ?? "Unknown",
                Count = g.Count,
                Percentage = totalUsers > 0 ? Math.Round((double)g.Count / totalUsers * 100, 1) : 0
            }).ToList();

            // Most Reported Users (top 10)
            var mostReported = await _context.UserReports
                .GroupBy(r => r.ReportedUserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var mostReportedUsers = new List<TopUserDto>();
            foreach (var r in mostReported)
            {
                var user = await _context.Users.FindAsync(r.UserId);
                if (user != null)
                    mostReportedUsers.Add(new TopUserDto { UserId = r.UserId, FullName = user.FullName, Email = user.Email, Count = r.Count });
            }

            // Most Active Users (by messages sent, top 10)
            var mostActive = await _context.Messages
                .Where(m => !m.IsDeleted)
                .GroupBy(m => m.SenderId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var mostActiveUsers = new List<TopUserDto>();
            foreach (var a in mostActive)
            {
                var user = await _context.Users.FindAsync(a.UserId);
                if (user != null)
                    mostActiveUsers.Add(new TopUserDto { UserId = a.UserId, FullName = user.FullName, Email = user.Email, Count = a.Count });
            }

            // Most Matched Users (top 10)
            var mostMatched = await _context.Matches
                .Where(m => m.IsMatch)
                .GroupBy(m => m.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            var mostMatchedUsers = new List<TopUserDto>();
            foreach (var m in mostMatched)
            {
                var user = await _context.Users.FindAsync(m.UserId);
                if (user != null)
                    mostMatchedUsers.Add(new TopUserDto { UserId = m.UserId, FullName = user.FullName, Email = user.Email, Count = m.Count });
            }

            return new AdvancedAnalytics
            {
                GenderDistribution = genderDist,
                AgeDistribution = ageDist,
                CountryDistribution = countryDist,
                CityDistribution = cityDist,
                MostReportedUsers = mostReportedUsers,
                MostActiveUsers = mostActiveUsers,
                MostMatchedUsers = mostMatchedUsers,
                TotalBannedUsers = await _context.Users.CountAsync(u => u.IsBanned)
            };
        }

        // ===== GET ALL USERS =====
        public async Task<PaginatedResult<AdminUserDto>> GetAllUsersAsync(int page, int pageSize, string? search, bool? isBanned)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            if (isBanned.HasValue)
                query = query.Where(u => u.IsBanned == isBanned.Value);

            var totalCount = await query.CountAsync();
            var users = await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var userDtos = new List<AdminUserDto>();
            foreach (var user in users)
            {
                userDtos.Add(new AdminUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Gender = user.Gender,
                    Age = user.Age,
                    City = user.City,
                    Country = user.Country,
                    IsProfileComplete = user.IsProfileComplete,
                    IsEmailVerified = user.IsEmailVerified,
                    IsBanned = user.IsBanned,
                    BanReason = user.BanReason,
                    BannedAt = user.BannedAt,
                    IsAdmin = user.IsAdmin,
                    CreatedAt = user.CreatedAt,
                    LastActive = user.LastActive,
                    TotalMatches = await _context.Matches.CountAsync(m => m.UserId == user.Id && m.IsMatch),
                    TotalMessages = await _context.Messages.CountAsync(m => m.SenderId == user.Id && !m.IsDeleted),
                    ReportsReceived = await _context.UserReports.CountAsync(r => r.ReportedUserId == user.Id)
                });
            }

            return new PaginatedResult<AdminUserDto> { Items = userDtos, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        // ===== GET USER DETAIL =====
        public async Task<AdminUserDetailDto?> GetUserDetailAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // Photos
            var photos = await _context.Photos.Where(p => p.UserId == userId).Select(p => p.Url).ToListAsync();

            // Matches
            var matchIds = await _context.Matches.Where(m => m.UserId == userId && m.IsMatch).ToListAsync();
            var matches = new List<AdminMatchDto>();
            foreach (var m in matchIds)
            {
                var matchedUser = await _context.Users.FindAsync(m.TargetUserId);
                if (matchedUser != null)
                    matches.Add(new AdminMatchDto
                    {
                        MatchedUserId = matchedUser.Id,
                        MatchedUserName = matchedUser.FullName,
                        MatchedUserEmail = matchedUser.Email,
                        MatchedAt = m.MatchedAt ?? DateTime.MinValue
                    });
            }

            // Recent Messages
            var recentMsgs = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt)
                .Take(20)
                .ToListAsync();

            var messageDtos = new List<AdminMessageDto>();
            foreach (var msg in recentMsgs)
            {
                var sender = await _context.Users.FindAsync(msg.SenderId);
                var receiver = await _context.Users.FindAsync(msg.ReceiverId);
                messageDtos.Add(new AdminMessageDto
                {
                    Id = msg.Id,
                    SenderId = msg.SenderId,
                    SenderName = sender?.FullName ?? "Unknown",
                    ReceiverId = msg.ReceiverId,
                    ReceiverName = receiver?.FullName ?? "Unknown",
                    Content = msg.Content,
                    SentAt = msg.SentAt,
                    IsDeleted = msg.IsDeleted
                });
            }

            return new AdminUserDetailDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Gender = user.Gender,
                Age = user.Age,
                City = user.City,
                Country = user.Country,
                IsProfileComplete = user.IsProfileComplete,
                IsEmailVerified = user.IsEmailVerified,
                IsBanned = user.IsBanned,
                BanReason = user.BanReason,
                BannedAt = user.BannedAt,
                IsAdmin = user.IsAdmin,
                CreatedAt = user.CreatedAt,
                LastActive = user.LastActive,
                TotalMatches = matches.Count,
                TotalMessages = await _context.Messages.CountAsync(m => m.SenderId == userId && !m.IsDeleted),
                ReportsReceived = await _context.UserReports.CountAsync(r => r.ReportedUserId == userId),
                Photos = photos,
                Matches = matches,
                RecentMessages = messageDtos,
                Hobbies = JsonSerializer.Deserialize<List<string>>(user.Hobbies) ?? new(),
                Interests = JsonSerializer.Deserialize<List<string>>(user.Interests) ?? new(),
                Bio = user.Bio,
                Occupation = user.Occupation,
                Education = user.Education,
                Height = user.Height
            };
        }

        // ===== BAN USER =====
        public async Task<AdminResponse> BanUserAsync(int adminId, BanUserRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return new AdminResponse { Success = false, Message = "User not found" };
            if (user.IsAdmin) return new AdminResponse { Success = false, Message = "Cannot ban an admin" };

            user.IsBanned = true;
            user.BanReason = request.Reason;
            user.BannedAt = DateTime.UtcNow;
            user.BannedBy = adminId;

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "BAN_USER", "User", user.Id, user.FullName, request.Reason);
            return new AdminResponse { Success = true, Message = $"{user.FullName} has been banned" };
        }

        // ===== UNBAN USER =====
        public async Task<AdminResponse> UnbanUserAsync(int adminId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new AdminResponse { Success = false, Message = "User not found" };

            user.IsBanned = false;
            user.BanReason = null;
            user.BannedAt = null;
            user.BannedBy = null;

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "UNBAN_USER", "User", user.Id, user.FullName);
            return new AdminResponse { Success = true, Message = $"{user.FullName} has been unbanned" };
        }

        // ===== DELETE USER =====
        public async Task<AdminResponse> DeleteUserAsync(int adminId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new AdminResponse { Success = false, Message = "User not found" };
            if (user.IsAdmin) return new AdminResponse { Success = false, Message = "Cannot delete an admin" };

            _context.Photos.RemoveRange(await _context.Photos.Where(p => p.UserId == userId).ToListAsync());
            _context.Matches.RemoveRange(await _context.Matches.Where(m => m.UserId == userId || m.TargetUserId == userId).ToListAsync());
            _context.Messages.RemoveRange(await _context.Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId).ToListAsync());
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "DELETE_USER", "User", userId, user.FullName);
            return new AdminResponse { Success = true, Message = "User deleted successfully" };
        }

        // ===== MAKE ADMIN =====
        public async Task<AdminResponse> MakeAdminAsync(int adminId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new AdminResponse { Success = false, Message = "User not found" };

            user.IsAdmin = true;
            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "MAKE_ADMIN", "User", user.Id, user.FullName);
            return new AdminResponse { Success = true, Message = $"{user.FullName} is now an admin" };
        }

        // ===== REMOVE ADMIN =====
        public async Task<AdminResponse> RemoveAdminAsync(int adminId, int userId)
        {
            if (adminId == userId) return new AdminResponse { Success = false, Message = "Cannot remove your own admin role" };

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new AdminResponse { Success = false, Message = "User not found" };

            user.IsAdmin = false;
            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "REMOVE_ADMIN", "User", user.Id, user.FullName);
            return new AdminResponse { Success = true, Message = $"{user.FullName} admin role removed" };
        }

        // ===== BULK BAN =====
        public async Task<BulkActionResult> BulkBanUsersAsync(int adminId, BulkBanRequest request)
        {
            int processed = 0, failed = 0;
            var errors = new List<string>();

            foreach (var userId in request.UserIds)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) { failed++; errors.Add($"User {userId} not found"); continue; }
                if (user.IsAdmin) { failed++; errors.Add($"Cannot ban admin {user.FullName}"); continue; }

                user.IsBanned = true;
                user.BanReason = request.Reason;
                user.BannedAt = DateTime.UtcNow;
                user.BannedBy = adminId;
                processed++;
            }

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "BULK_BAN", "User", null, null, $"Banned {processed} users. Reason: {request.Reason}");

            return new BulkActionResult
            {
                Success = true,
                Message = $"Banned {processed} users, {failed} failed",
                ProcessedCount = processed,
                FailedCount = failed,
                Errors = errors
            };
        }

        // ===== BULK DELETE =====
        public async Task<BulkActionResult> BulkDeleteUsersAsync(int adminId, BulkDeleteRequest request)
        {
            int processed = 0, failed = 0;
            var errors = new List<string>();

            foreach (var userId in request.UserIds)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) { failed++; errors.Add($"User {userId} not found"); continue; }
                if (user.IsAdmin) { failed++; errors.Add($"Cannot delete admin {user.FullName}"); continue; }

                _context.Photos.RemoveRange(await _context.Photos.Where(p => p.UserId == userId).ToListAsync());
                _context.Matches.RemoveRange(await _context.Matches.Where(m => m.UserId == userId || m.TargetUserId == userId).ToListAsync());
                _context.Messages.RemoveRange(await _context.Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId).ToListAsync());
                _context.Users.Remove(user);
                processed++;
            }

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "BULK_DELETE", "User", null, null, $"Deleted {processed} users");

            return new BulkActionResult
            {
                Success = true,
                Message = $"Deleted {processed} users, {failed} failed",
                ProcessedCount = processed,
                FailedCount = failed,
                Errors = errors
            };
        }

        // ===== GET REPORTS =====
        public async Task<PaginatedResult<AdminReportDto>> GetReportsAsync(int page, int pageSize, bool? isResolved)
        {
            var query = _context.UserReports.AsQueryable();
            if (isResolved.HasValue) query = query.Where(r => r.IsResolved == isResolved.Value);

            var totalCount = await query.CountAsync();
            var reports = await query.OrderByDescending(r => r.ReportedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var reportDtos = new List<AdminReportDto>();
            foreach (var report in reports)
            {
                var reporter = await _context.Users.FindAsync(report.ReporterId);
                var reported = await _context.Users.FindAsync(report.ReportedUserId);
                reportDtos.Add(new AdminReportDto
                {
                    Id = report.Id,
                    ReporterId = report.ReporterId,
                    ReporterName = reporter?.FullName ?? "Unknown",
                    ReporterEmail = reporter?.Email ?? "Unknown",
                    ReportedUserId = report.ReportedUserId,
                    ReportedUserName = reported?.FullName ?? "Unknown",
                    ReportedUserEmail = reported?.Email ?? "Unknown",
                    Reason = report.Reason.ToString(),
                    Description = report.Description,
                    ReportedAt = report.ReportedAt,
                    IsResolved = report.IsResolved
                });
            }

            return new PaginatedResult<AdminReportDto> { Items = reportDtos, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        // ===== RESOLVE REPORT =====
        public async Task<AdminResponse> ResolveReportAsync(int adminId, ResolveReportRequest request)
        {
            var report = await _context.UserReports.FindAsync(request.ReportId);
            if (report == null) return new AdminResponse { Success = false, Message = "Report not found" };

            report.IsResolved = true;

            if (request.BanReportedUser)
            {
                var user = await _context.Users.FindAsync(report.ReportedUserId);
                if (user != null)
                {
                    user.IsBanned = true;
                    user.BanReason = $"Banned: {request.AdminNote}";
                    user.BannedAt = DateTime.UtcNow;
                    user.BannedBy = adminId;
                }
            }

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "RESOLVE_REPORT", "Report", report.Id, null, request.AdminNote);
            return new AdminResponse { Success = true, Message = "Report resolved" };
        }

        // ===== DELETE MESSAGE =====
        public async Task<AdminResponse> DeleteMessageAsync(int adminId, int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return new AdminResponse { Success = false, Message = "Message not found" };

            message.IsDeleted = true;
            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "DELETE_MESSAGE", "Message", messageId);
            return new AdminResponse { Success = true, Message = "Message deleted" };
        }

        // ===== DELETE PHOTO =====
        public async Task<AdminResponse> DeletePhotoAsync(int adminId, int photoId)
        {
            var photo = await _context.Photos.FindAsync(photoId);
            if (photo == null) return new AdminResponse { Success = false, Message = "Photo not found" };

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "DELETE_PHOTO", "Photo", photoId);
            return new AdminResponse { Success = true, Message = "Photo deleted" };
        }

        // ===== GET ALL MESSAGES =====
        public async Task<PaginatedResult<AdminMessageDto>> GetAllMessagesAsync(int page, int pageSize, int? userId)
        {
            var query = _context.Messages.AsQueryable();
            if (userId.HasValue)
                query = query.Where(m => m.SenderId == userId || m.ReceiverId == userId);

            var totalCount = await query.CountAsync();
            var messages = await query.OrderByDescending(m => m.SentAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var dtos = new List<AdminMessageDto>();
            foreach (var msg in messages)
            {
                var sender = await _context.Users.FindAsync(msg.SenderId);
                var receiver = await _context.Users.FindAsync(msg.ReceiverId);
                dtos.Add(new AdminMessageDto
                {
                    Id = msg.Id,
                    SenderId = msg.SenderId,
                    SenderName = sender?.FullName ?? "Unknown",
                    ReceiverId = msg.ReceiverId,
                    ReceiverName = receiver?.FullName ?? "Unknown",
                    Content = msg.Content,
                    SentAt = msg.SentAt,
                    IsDeleted = msg.IsDeleted
                });
            }

            return new PaginatedResult<AdminMessageDto> { Items = dtos, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        // ===== ACTIVITY LOGS =====
        public async Task<PaginatedResult<AdminActivityLog>> GetActivityLogsAsync(int page, int pageSize, int? adminId)
        {
            var query = _context.AdminLogs.AsQueryable();
            if (adminId.HasValue) query = query.Where(l => l.AdminId == adminId.Value);

            var totalCount = await query.CountAsync();
            var logs = await query.OrderByDescending(l => l.PerformedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var dtos = new List<AdminActivityLog>();
            foreach (var log in logs)
            {
                var admin = await _context.Users.FindAsync(log.AdminId);
                dtos.Add(new AdminActivityLog
                {
                    Id = log.Id,
                    AdminId = log.AdminId,
                    AdminName = admin?.FullName ?? "Unknown",
                    Action = log.Action,
                    TargetType = log.TargetType,
                    TargetId = log.TargetId,
                    TargetName = log.TargetName,
                    Details = log.Details,
                    PerformedAt = log.PerformedAt
                });
            }

            return new PaginatedResult<AdminActivityLog> { Items = dtos, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        // ===== APP SETTINGS =====
        public async Task<List<AppSetting>> GetAllSettingsAsync()
        {
            var settings = await _context.AppSettings.ToListAsync();

            // Seed default settings if empty
            if (!settings.Any())
            {
                var defaults = new List<AppSetting>
                {
                    new() { Key = "MaintenanceMode", Value = "false", Description = "Put app in maintenance mode" },
                    new() { Key = "AllowNewRegistrations", Value = "true", Description = "Allow new user registrations" },
                    new() { Key = "MaxPhotosPerUser", Value = "6", Description = "Maximum photos per user" },
                    new() { Key = "MaxDailySwipes", Value = "100", Description = "Maximum swipes per day" },
                    new() { Key = "RequireEmailVerification", Value = "false", Description = "Require email verification" },
                    new() { Key = "AppVersion", Value = "1.0.0", Description = "Current app version" },
                };
                _context.AppSettings.AddRange(defaults);
                await _context.SaveChangesAsync();
                return defaults;
            }

            return settings;
        }

        public async Task<AdminResponse> UpdateSettingsAsync(int adminId, UpdateSettingsRequest request)
        {
            foreach (var item in request.Settings)
            {
                var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == item.Key);
                if (setting != null)
                {
                    setting.Value = item.Value;
                    setting.UpdatedAt = DateTime.UtcNow;
                    setting.UpdatedBy = adminId;
                }
                else
                {
                    _context.AppSettings.Add(new AppSetting
                    {
                        Key = item.Key,
                        Value = item.Value,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = adminId
                    });
                }
            }

            await _context.SaveChangesAsync();
            await LogActionAsync(adminId, "UPDATE_SETTINGS", "Settings", null, null, $"Updated {request.Settings.Count} settings");
            return new AdminResponse { Success = true, Message = "Settings updated successfully" };
        }

        // ===== EXPORT TO CSV =====
        public async Task<byte[]> ExportUsersToCsvAsync()
        {
            var users = await _context.Users.ToListAsync();
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("Id,FullName,Email,Gender,Age,City,Country,IsProfileComplete,IsBanned,IsAdmin,CreatedAt,LastActive,TotalMatches,TotalMessages,ReportsReceived");

            // Rows
            foreach (var user in users)
            {
                var matches = await _context.Matches.CountAsync(m => m.UserId == user.Id && m.IsMatch);
                var messages = await _context.Messages.CountAsync(m => m.SenderId == user.Id && !m.IsDeleted);
                var reports = await _context.UserReports.CountAsync(r => r.ReportedUserId == user.Id);

                sb.AppendLine($"{user.Id}," +
                    $"\"{user.FullName}\"," +
                    $"\"{user.Email}\"," +
                    $"{user.Gender ?? ""}," +
                    $"{user.Age?.ToString() ?? ""}," +
                    $"\"{user.City ?? ""}\"," +
                    $"\"{user.Country ?? ""}\"," +
                    $"{user.IsProfileComplete}," +
                    $"{user.IsBanned}," +
                    $"{user.IsAdmin}," +
                    $"{user.CreatedAt:yyyy-MM-dd HH:mm}," +
                    $"{user.LastActive?.ToString("yyyy-MM-dd HH:mm") ?? ""}," +
                    $"{matches},{messages},{reports}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}