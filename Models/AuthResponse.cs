using System;
namespace AuthAPI.Models
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }      // JWT token (nullable)
        public UserDto? User { get; set; }      // User info (nullable)
    }
}

