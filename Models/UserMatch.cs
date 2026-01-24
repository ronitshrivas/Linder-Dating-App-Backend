using System;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class UserMatchDto
    {
        public UserDto User { get; set; } = new UserDto();
        public double MatchScore { get; set; } // 0-100
        public MatchBreakdown Breakdown { get; set; } = new MatchBreakdown();
    }

    public class MatchBreakdown
    {
        public double InterestScore { get; set; }
        public double HobbyScore { get; set; }
        public double HoroscopeScore { get; set; }
        public double AgeCompatibility { get; set; }
        public double DistanceScore { get; set; }
        public List<string> CommonInterests { get; set; } = new List<string>();
        public List<string> CommonHobbies { get; set; } = new List<string>();
    }
}