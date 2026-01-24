using System.Collections.Generic;

namespace AuthAPI.Models
{
    public static class AppConstants
    {
        // Available Hobbies
        public static readonly List<string> AvailableHobbies = new List<string>
        {
            "Reading", "Writing", "Traveling", "Photography", "Cooking",
            "Gaming", "Sports", "Fitness", "Yoga", "Meditation",
            "Music", "Dancing", "Singing", "Painting", "Drawing",
            "Hiking", "Camping", "Swimming", "Cycling", "Running",
            "Movies", "Theatre", "Netflix", "Podcasts", "Blogging",
            "Gardening", "Pet Care", "Volunteering", "Shopping", "Fashion"
        };

        // Available Interests
        public static readonly List<string> AvailableInterests = new List<string>
        {
            "Technology", "Science", "Art", "Literature", "History",
            "Politics", "Environment", "Health & Wellness", "Finance",
            "Business", "Entrepreneurship", "Psychology", "Philosophy",
            "Spirituality", "Astrology", "Food & Dining", "Wine & Spirits",
            "Coffee Culture", "Nightlife", "Adventure Sports", "Luxury Travel",
            "Budget Travel", "Language Learning", "Career Development",
            "Social Causes", "Animal Rights", "Sustainability"
        };

        // Hindu Nakshatras (27 birth stars)
        public static readonly List<string> Nakshatras = new List<string>
        {
            "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira",
            "Ardra", "Punarvasu", "Pushya", "Ashlesha", "Magha",
            "Purva Phalguni", "Uttara Phalguni", "Hasta", "Chitra", "Swati",
            "Vishakha", "Anuradha", "Jyeshtha", "Mula", "Purva Ashadha",
            "Uttara Ashadha", "Shravana", "Dhanishta", "Shatabhisha",
            "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
        };

        // Hindu Rashi (Vedic Zodiac Signs)
        public static readonly List<string> RashiSigns = new List<string>
        {
            "Mesha (Aries)", "Vrishabha (Taurus)", "Mithuna (Gemini)",
            "Karka (Cancer)", "Simha (Leo)", "Kanya (Virgo)",
            "Tula (Libra)", "Vrishchika (Scorpio)", "Dhanu (Sagittarius)",
            "Makara (Capricorn)", "Kumbha (Aquarius)", "Meena (Pisces)"
        };

        // Western Zodiac Signs
        public static readonly List<string> ZodiacSigns = new List<string>
        {
            "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
            "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces"
        };

        // Chinese Zodiac
        public static readonly List<string> ChineseZodiacSigns = new List<string>
        {
            "Rat", "Ox", "Tiger", "Rabbit", "Dragon", "Snake",
            "Horse", "Goat", "Monkey", "Rooster", "Dog", "Pig"
        };
    }
}