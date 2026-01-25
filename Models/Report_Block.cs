using System;

namespace AuthAPI.Models
{
    public class UserReport
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public int ReportedUserId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;

        // Navigation properties
        public User? Reporter { get; set; }
        public User? ReportedUser { get; set; }
    }

    public class UserBlock
    {
        public int Id { get; set; }
        public int BlockerId { get; set; }
        public int BlockedUserId { get; set; }
        public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? Blocker { get; set; }
        public User? BlockedUser { get; set; }
    }

    public enum ReportReason
    {
        InappropriateContent,
        Harassment,
        FakeProfile,
        Spam,
        Scam,
        UnderAge,
        Other
    }

    public class ReportUserRequest
    {
        public int ReportedUserId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
    }

    public class BlockUserRequest
    {
        public int BlockedUserId { get; set; }
    }
}