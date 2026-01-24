using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;

        public int MaxDistance { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        public List<string> ProfilePhotos { get; set; } = new List<string>();
        public List<string> Hobbies { get; set; } = new List<string>();
        public List<string> Interests { get; set; } = new List<string>();

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

        public bool IsProfileComplete { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}