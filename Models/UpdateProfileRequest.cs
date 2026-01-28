using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class UpdateProfileRequest
    {
        // Basic Info
        public string? FullName { get; set; }

        // Personal Details
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? InterestedIn { get; set; } // For "Prefer not to say" gender

        // Location & Preferences
        public int? MaxDistance { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }

        // Age Preferences
        public int? PreferredAgeMin { get; set; }
        public int? PreferredAgeMax { get; set; }

        // Interests & Hobbies
        public List<string>? Hobbies { get; set; }
        public List<string>? Interests { get; set; }

        // Horoscope Details
        public string? ZodiacSign { get; set; }
        public string? SunSign { get; set; }
        public string? MoonSign { get; set; }
        public string? RashiSign { get; set; }
        public string? Nakshatra { get; set; }
        public string? ChineseZodiac { get; set; }

        // Additional Profile Info
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }
        public int? Height { get; set; }
    }
}