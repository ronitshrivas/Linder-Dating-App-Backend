using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        // ===== STEP 1: BASIC INFO (REQUIRED) =====
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // ===== STEP 2: PERSONAL DETAILS (OPTIONAL until profile completion) =====

        // Nullable DateTime for DOB (only set in Step 2)
        public DateTime? DateOfBirth { get; set; }

        // Nullable int for Age (calculated from DOB in Step 2)
        public int? Age { get; set; }

        // Nullable string for Gender (only set in Step 2)
        public string? Gender { get; set; }

        // Nullable - only for "Prefer not to say" gender
        public string? InterestedIn { get; set; }

        // ===== LOCATION & PREFERENCES (OPTIONAL until Step 2) =====

        // Nullable int with default value for MaxDistance
        public int? MaxDistance { get; set; }

        // Nullable address (only set in Step 2)
        public string? Address { get; set; }

        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }

        // ===== AGE PREFERENCES (OPTIONAL until Step 2) =====

        public int? PreferredAgeMin { get; set; }
        public int? PreferredAgeMax { get; set; }

        // ===== PROFILE PHOTOS (OPTIONAL until Step 2) =====

        // Empty array by default, filled in Step 2
        public string ProfilePhotos { get; set; } = "[]"; 

        // ===== OPTIONAL FIELDS (Can remain empty even after Step 2) =====

        public string Hobbies { get; set; } = "[]"; // JSON array
        public string Interests { get; set; } = "[]"; // JSON array
        public string? ZodiacSign { get; set; }
        public string? SunSign { get; set; }
        public string? MoonSign { get; set; }
        public string? RashiSign { get; set; }
        public string? Nakshatra { get; set; }
        public string? ChineseZodiac { get; set; }
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }
        public int? Height { get; set; }

        // ===== ACCOUNT STATUS =====

        public bool IsProfileComplete { get; set; } = false;
        public bool IsEmailVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActive { get; set; }
    }
}