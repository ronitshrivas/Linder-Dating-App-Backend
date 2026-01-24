using System;

namespace AuthAPI.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Who swiped
        public int TargetUserId { get; set; } // Who they swiped on
        public SwipeAction Action { get; set; } // Like or Pass
        public DateTime SwipedAt { get; set; } = DateTime.UtcNow;
        public bool IsMatch { get; set; } = false; // True if both liked each other

        // Navigation properties
        public User? User { get; set; }
        public User? TargetUser { get; set; }
    }

    public enum SwipeAction
    {
        Pass = 0,  // Swipe Left
        Like = 1   // Swipe Right
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
        public bool IsMatch { get; set; } = false;
        public UserDto? MatchedUser { get; set; }
    }
}