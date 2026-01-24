using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        // Basic Info
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Personal Details
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty; // Male, Female, Other
        public int Age { get; set; } // Calculated from DOB

        // Location & Preferences
        public int MaxDistance { get; set; } // In kilometers
        public string? City { get; set; }
        public string? State { get; set; }

        // Profile Images (URLs stored as JSON string or separate table)
        public string ProfilePhotos { get; set; } = "[]"; // JSON array of photo URLs

        // Interests & Hobbies
        public string Hobbies { get; set; } = "[]"; // JSON array
        public string Interests { get; set; } = "[]"; // JSON array

        // Horoscope Details
        public string ZodiacSign { get; set; } = string.Empty; // Western zodiac
        public string SunSign { get; set; } = string.Empty;
        public string MoonSign { get; set; } = string.Empty;
        public string RashiSign { get; set; } = string.Empty; // Hindu/Vedic zodiac
        public string Nakshatra { get; set; } = string.Empty; // Hindu birth star
        public string ChineseZodiac { get; set; } = string.Empty;

        // Additional Profile Info
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }
        public int? Height { get; set; } // In centimeters

        // Account Status
        public bool IsProfileComplete { get; set; } = false;
        public bool IsEmailVerified { get; set; } = false;
        public bool IsPhoneVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActive { get; set; }
    }
}