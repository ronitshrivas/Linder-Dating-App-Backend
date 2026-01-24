using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Models
{
    public class SignupRequest
    {
        // Basic Info
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Personal Details
        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; } = string.Empty;

        // Location & Preferences
        [Range(1, 500, ErrorMessage = "Distance must be between 1 and 500 km")]
        public int MaxDistance { get; set; } = 50; // Default 50km

        public string? City { get; set; }
        public string? State { get; set; }

        // Profile Photos (minimum 6 required)
        [Required(ErrorMessage = "At least 6 photos are required")]
        [MinLength(6, ErrorMessage = "You must upload at least 6 photos")]
        public List<string> ProfilePhotos { get; set; } = new List<string>();

        // Interests & Hobbies
        [Required(ErrorMessage = "Please select at least one hobby")]
        [MinLength(1, ErrorMessage = "Please select at least one hobby")]
        public List<string> Hobbies { get; set; } = new List<string>();

        [Required(ErrorMessage = "Please select at least one interest")]
        [MinLength(1, ErrorMessage = "Please select at least one interest")]
        public List<string> Interests { get; set; } = new List<string>();

        // Horoscope Details
        public string? ZodiacSign { get; set; }
        public string? SunSign { get; set; }
        public string? MoonSign { get; set; }
        public string? RashiSign { get; set; }
        public string? Nakshatra { get; set; }
        public string? ChineseZodiac { get; set; }

        // Optional Profile Info
        public string? Bio { get; set; }
        public string? Occupation { get; set; }
        public string? Education { get; set; }

        [Range(100, 250, ErrorMessage = "Height must be between 100 and 250 cm")]
        public int? Height { get; set; }
    }
}