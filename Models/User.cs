using System;
namespace AuthAPI.Models
{
    public class User
    {
        public int Id { get; set; }                    // Primary key
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;  // Never store plain password!
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

