using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public int? MaxDistance { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public List<string>? Hobbies { get; set; }
        public List<string>? Interests { get; set; }
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
    }
}