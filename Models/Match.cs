using System;

namespace AuthAPI.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TargetUserId { get; set; }
        public SwipeAction Action { get; set; }
        public DateTime SwipedAt { get; set; }
        public bool IsMatch { get; set; }
        public DateTime? MatchedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public User? TargetUser { get; set; }
    }

    public enum SwipeAction
    {
        Like,
        Pass,
        SuperLike
    }

    public class SwipeRequest
    {
        public int TargetUserId { get; set; }
        public SwipeAction Action { get; set; }
    }

    public class SwipeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsMatch { get; set; }
        public UserDto? MatchedUser { get; set; }
    }
}