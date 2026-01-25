using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        // Step 1: Basic Info (REQUIRED)
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Step 2: Personal Details (REQUIRED)
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty; // Male, Female, Other, Prefer not to say
        public string? InterestedIn { get; set; } // Male, Female, Both (for "Prefer not to say")

        // Location & Preferences (REQUIRED)
        public int MaxDistance { get; set; } = 50;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }

        // Age Preferences (REQUIRED)
        public int PreferredAgeMin { get; set; } = 18;
        public int PreferredAgeMax { get; set; } = 80;

        // Profile Photos (REQUIRED - at least 2)
        public string ProfilePhotos { get; set; } = "[]"; // JSON array

        // OPTIONAL Fields
        public string Hobbies { get; set; } = "[]"; // JSON array
        public string Interests { get; set; } = "[]"; // JSON array
        public string ZodiacSign { get; set; } = string.Empty;
        public string SunSign { get; set; } = string.Empty;
        public string MoonSign { get; set; } = string.Empty;
        public string RashiSign { get; set; } = string.Empty;
        public string Nakshatra { get; set; } = string.Empty;
        public string ChineseZodiac { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }
        public int? Height { get; set; }

        // Account Status
        public bool IsProfileComplete { get; set; } = false;
        public bool IsEmailVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActive { get; set; }
    }
}