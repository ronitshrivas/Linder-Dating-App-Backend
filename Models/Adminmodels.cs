using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    // ===== ADMIN USER DTO =====
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsProfileComplete { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BannedAt { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActive { get; set; }
        public int TotalMatches { get; set; }
        public int TotalMessages { get; set; }
        public int ReportsReceived { get; set; }
    }

    // ===== USER FULL DETAIL =====
    public class AdminUserDetailDto : AdminUserDto
    {
        public List<string> Photos { get; set; } = new();
        public List<AdminMatchDto> Matches { get; set; } = new();
        public List<AdminMessageDto> RecentMessages { get; set; } = new();
        public List<string> Hobbies { get; set; } = new();
        public List<string> Interests { get; set; } = new();
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }
        public int? Height { get; set; }
    }

    public class AdminMatchDto
    {
        public int MatchedUserId { get; set; }
        public string MatchedUserName { get; set; } = string.Empty;
        public string MatchedUserEmail { get; set; } = string.Empty;
        public DateTime MatchedAt { get; set; }
    }

    public class AdminMessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    // ===== DASHBOARD STATS =====
    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int ActiveUsersToday { get; set; }
        public int TotalMatches { get; set; }
        public int TotalMessages { get; set; }
        public int PendingReports { get; set; }
        public int ResolvedReports { get; set; }
        public int BannedUsers { get; set; }
        public int ProfilesComplete { get; set; }
        public List<DailyStats> Last7Days { get; set; } = new();
    }

    public class DailyStats
    {
        public string Date { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int NewMatches { get; set; }
        public int NewMessages { get; set; }
    }

    // ===== ADVANCED ANALYTICS =====
    public class AdvancedAnalytics
    {
        public List<DistributionItem> GenderDistribution { get; set; } = new();
        public List<DistributionItem> AgeDistribution { get; set; } = new();
        public List<DistributionItem> CountryDistribution { get; set; } = new();
        public List<DistributionItem> CityDistribution { get; set; } = new();
        public List<TopUserDto> MostReportedUsers { get; set; } = new();
        public List<TopUserDto> MostActiveUsers { get; set; } = new();
        public List<TopUserDto> MostMatchedUsers { get; set; } = new();
        public double AverageMatchRate { get; set; }
        public double AverageProfileCompletion { get; set; }
        public int TotalBannedUsers { get; set; }
    }

    public class DistributionItem
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class TopUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // ===== BAN USER =====
    public class BanUserRequest
    {
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // ===== BULK ACTIONS =====
    public class BulkBanRequest
    {
        public List<int> UserIds { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    public class BulkDeleteRequest
    {
        public List<int> UserIds { get; set; } = new();
    }

    public class BulkActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // ===== RESOLVE REPORT =====
    public class ResolveReportRequest
    {
        public int ReportId { get; set; }
        public string AdminNote { get; set; } = string.Empty;
        public bool BanReportedUser { get; set; } = false;
    }

    // ===== ADMIN REPORT DTO =====
    public class AdminReportDto
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public int ReportedUserId { get; set; }
        public string ReportedUserName { get; set; } = string.Empty;
        public string ReportedUserEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReportedAt { get; set; }
        public bool IsResolved { get; set; }
    }

    // ===== ADMIN ACTIVITY LOG =====
    public class AdminActivityLog
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? Details { get; set; }
        public DateTime PerformedAt { get; set; }
    }

    // DB Model for activity log
    public class AdminLog
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? Details { get; set; }
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
        public User? Admin { get; set; }
    }

    // ===== APP SETTINGS =====
    public class AppSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
    }

    public class UpdateSettingRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateSettingsRequest
    {
        public List<UpdateSettingRequest> Settings { get; set; } = new();
    }

    // ===== PAGINATED RESULT =====
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // ===== ADMIN RESPONSE =====
    public class AdminResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}