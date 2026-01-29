using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Models
{
    // STEP 2: Complete Profile Request - ALL FIELDS ARE 100% OPTIONAL
    // User can skip everything and still use the app
    public class CompleteProfileRequest
    {
        // ===== ALL FIELDS ARE COMPLETELY OPTIONAL - NO VALIDATION =====

        // Profile Photos (completely optional, no minimum required)
        public List<string>? ProfilePhotos { get; set; }

        // Personal Details (all optional)
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? InterestedIn { get; set; } // "Male", "Female", "Both"

        // Location & Preferences (all optional)
        public int? MaxDistance { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }

        // Age Preferences (all optional)
        public int? PreferredAgeMin { get; set; }
        public int? PreferredAgeMax { get; set; }

        // Horoscope Details (all optional)
        public string? ZodiacSign { get; set; }
        public string? SunSign { get; set; }
        public string? MoonSign { get; set; }
        public string? RashiSign { get; set; }
        public string? Nakshatra { get; set; }
        public string? ChineseZodiac { get; set; }

        // Additional Info (all optional)
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }
        public int? Height { get; set; }

        // Interests & Hobbies (all optional)
        public List<string>? Hobbies { get; set; }
        public List<string>? Interests { get; set; }
    }

    // STEP 2: Complete Profile Response
    public class CompleteProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }

    // Profile Status DTO
    public class ProfileStatusDto
    {
        public bool IsProfileComplete { get; set; }
        public string CurrentStep { get; set; } = string.Empty; // "registered" or "completed"
        public List<string> MissingFields { get; set; } = new List<string>();
        public int CompletionPercentage { get; set; } // 0-100%
    }
}