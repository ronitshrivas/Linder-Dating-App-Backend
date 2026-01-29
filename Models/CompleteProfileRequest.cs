using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Models
{
    // STEP 2: Complete Profile Request - ALL FIELDS OPTIONAL
    public class CompleteProfileRequest
    {
        // ===== ALL FIELDS ARE OPTIONAL =====

        // Profile Photos (optional, but if provided, must be 2-6)
        [MinLength(2, ErrorMessage = "If uploading photos, please upload at least 2 (maximum 6)")]
        [MaxLength(6, ErrorMessage = "Maximum 6 photos allowed")]
        public List<string>? ProfilePhotos { get; set; }

        // Personal Details (all optional)
        public DateTime? DateOfBirth { get; set; }

        [RegularExpression("^(Male|Female|Other|Prefer not to say)$",
            ErrorMessage = "Gender must be: Male, Female, Other, or Prefer not to say")]
        public string? Gender { get; set; }

        // Only needed if Gender = "Prefer not to say"
        public string? InterestedIn { get; set; } // "Male", "Female", "Both"

        // Location & Preferences (all optional)
        [Range(1, 500, ErrorMessage = "Distance must be between 1 and 500 km")]
        public int? MaxDistance { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }

        // Age Preferences (all optional)
        [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
        public int? PreferredAgeMin { get; set; }

        [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
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

        [Range(100, 250, ErrorMessage = "Height must be between 100 and 250 cm")]
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