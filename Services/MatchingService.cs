using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthAPI.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly AppDbContext _context;


        // Add this method to the existing MatchingService.cs

        public class MatchStats
        {
            public int TotalMatches { get; set; }
            public int TotalLikes { get; set; }
            public int TotalPasses { get; set; }
            public int LikesReceived { get; set; }
            public int SuperLikes { get; set; }
            public double MatchRate { get; set; }
        }

        // Add this to IMatchingService interface
        //Task<MatchStats> GetMatchStatsAsync(int userId);

        // Add this implementation to MatchingService class
        public async Task<MatchStats> GetMatchStatsAsync(int userId)
        {
            var totalMatches = await _context.Matches
                .CountAsync(m => m.UserId == userId && m.IsMatch);

            var totalLikes = await _context.Matches
                .CountAsync(m => m.UserId == userId && m.Action == SwipeAction.Like);

            var totalPasses = await _context.Matches
                .CountAsync(m => m.UserId == userId && m.Action == SwipeAction.Pass);

            var likesReceived = await _context.Matches
                .CountAsync(m => m.TargetUserId == userId && m.Action == SwipeAction.Like);

            var superLikes = await _context.Matches
                .CountAsync(m => m.UserId == userId && m.Action == SwipeAction.SuperLike);

            var matchRate = totalLikes > 0 ? (double)totalMatches / totalLikes * 100 : 0;

            return new MatchStats
            {
                TotalMatches = totalMatches,
                TotalLikes = totalLikes,
                TotalPasses = totalPasses,
                LikesReceived = likesReceived,
                SuperLikes = superLikes,
                MatchRate = Math.Round(matchRate, 2)
            };
        }


        public MatchingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserMatchDto>> GetPotentialMatchesAsync(int userId, int limit = 20)
        {
            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null)
                return new List<UserMatchDto>();

            // Get users already swiped on
            var swipedUserIds = await _context.Matches
                .Where(m => m.UserId == userId)
                .Select(m => m.TargetUserId)
                .ToListAsync();

            // Get potential matches (exclude already swiped and self)
            var potentialUsers = await _context.Users
                .Where(u => u.Id != userId && !swipedUserIds.Contains(u.Id))
                .Take(100) // Get more than needed for scoring
                .ToListAsync();

            // Calculate match scores
            var scoredMatches = potentialUsers
                .Select(user => new UserMatchDto
                {
                    User = MapToUserDto(user),
                    MatchScore = CalculateMatchScore(currentUser, user, out var breakdown),
                    Breakdown = breakdown
                })
                .OrderByDescending(m => m.MatchScore)
                .Take(limit)
                .ToList();

            return scoredMatches;
        }

        public async Task<SwipeResponse> SwipeAsync(int userId, SwipeRequest request)
        {
            // Check if already swiped
            var existingSwipe = await _context.Matches
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TargetUserId == request.TargetUserId);

            if (existingSwipe != null)
            {
                return new SwipeResponse
                {
                    Success = false,
                    Message = "Already swiped on this user"
                };
            }

            // Create swipe record
            var match = new Match
            {
                UserId = userId,
                TargetUserId = request.TargetUserId,
                Action = request.Action,
                SwipedAt = DateTime.UtcNow,
                IsMatch = false
            };

            _context.Matches.Add(match);

            // If it's a LIKE, check if there's a mutual match
            if (request.Action == SwipeAction.Like)
            {
                var reverseMatch = await _context.Matches
                    .FirstOrDefaultAsync(m =>
                        m.UserId == request.TargetUserId &&
                        m.TargetUserId == userId &&
                        m.Action == SwipeAction.Like);

                if (reverseMatch != null)
                {
                    // It's a match!
                    match.IsMatch = true;
                    reverseMatch.IsMatch = true;

                    await _context.SaveChangesAsync();

                    var matchedUser = await _context.Users.FindAsync(request.TargetUserId);

                    return new SwipeResponse
                    {
                        Success = true,
                        Message = "It's a match! 🎉",
                        IsMatch = true,
                        MatchedUser = matchedUser != null ? MapToUserDto(matchedUser) : null
                    };
                }
            }

            await _context.SaveChangesAsync();

            return new SwipeResponse
            {
                Success = true,
                Message = request.Action == SwipeAction.Like ? "Liked!" : "Passed",
                IsMatch = false
            };
        }

        public async Task<List<UserDto>> GetMyMatchesAsync(int userId)
        {
            // Get all mutual matches
            var matchedUserIds = await _context.Matches
                .Where(m => m.UserId == userId && m.IsMatch)
                .Select(m => m.TargetUserId)
                .ToListAsync();

            var matchedUsers = await _context.Users
                .Where(u => matchedUserIds.Contains(u.Id))
                .ToListAsync();

            return matchedUsers.Select(MapToUserDto).ToList();
        }

        public async Task<List<UserDto>> GetLikesReceivedAsync(int userId)
        {
            // Get users who liked me (but I haven't swiped on yet)
            var likedMeUserIds = await _context.Matches
                .Where(m => m.TargetUserId == userId && m.Action == SwipeAction.Like && !m.IsMatch)
                .Select(m => m.UserId)
                .ToListAsync();

            var usersWhoLikedMe = await _context.Users
                .Where(u => likedMeUserIds.Contains(u.Id))
                .ToListAsync();

            return usersWhoLikedMe.Select(MapToUserDto).ToList();
        }

        public async Task<bool> UnmatchAsync(int userId, int matchedUserId)
        {
            var match1 = await _context.Matches
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TargetUserId == matchedUserId);

            var match2 = await _context.Matches
                .FirstOrDefaultAsync(m => m.UserId == matchedUserId && m.TargetUserId == userId);

            if (match1 != null)
                _context.Matches.Remove(match1);

            if (match2 != null)
                _context.Matches.Remove(match2);

            await _context.SaveChangesAsync();
            return true;
        }

        // Calculate match score (0-100)
        private double CalculateMatchScore(User currentUser, User targetUser, out MatchBreakdown breakdown)
        {
            breakdown = new MatchBreakdown();

            // Parse JSON arrays
            var currentHobbies = JsonSerializer.Deserialize<List<string>>(currentUser.Hobbies) ?? new List<string>();
            var targetHobbies = JsonSerializer.Deserialize<List<string>>(targetUser.Hobbies) ?? new List<string>();
            var currentInterests = JsonSerializer.Deserialize<List<string>>(currentUser.Interests) ?? new List<string>();
            var targetInterests = JsonSerializer.Deserialize<List<string>>(targetUser.Interests) ?? new List<string>();

            // 1. Interest Compatibility (30%)
            var commonInterests = currentInterests.Intersect(targetInterests).ToList();
            breakdown.CommonInterests = commonInterests;
            breakdown.InterestScore = currentInterests.Count > 0
                ? (double)commonInterests.Count / currentInterests.Count * 30
                : 0;

            // 2. Hobby Compatibility (25%)
            var commonHobbies = currentHobbies.Intersect(targetHobbies).ToList();
            breakdown.CommonHobbies = commonHobbies;
            breakdown.HobbyScore = currentHobbies.Count > 0
                ? (double)commonHobbies.Count / currentHobbies.Count * 25
                : 0;

            // 3. Horoscope Compatibility (20%)
            breakdown.HoroscopeScore = CalculateHoroscopeCompatibility(currentUser, targetUser);

            // 4. Age Compatibility (15%)
            var ageDiff = Math.Abs((decimal)(currentUser.Age - targetUser.Age));
            breakdown.AgeCompatibility = ageDiff <= 5 ? 15 :
                                         ageDiff <= 10 ? 10 :
                                         ageDiff <= 15 ? 5 : 0;

            // 5. Distance Score (10%) - assuming same city for now
            breakdown.DistanceScore = currentUser.City == targetUser.City ? 10 : 5;

            // Total score
            var totalScore = breakdown.InterestScore +
                           breakdown.HobbyScore +
                           breakdown.HoroscopeScore +
                           breakdown.AgeCompatibility +
                           breakdown.DistanceScore;

            return Math.Round(totalScore, 2);
        }

        private double CalculateHoroscopeCompatibility(User user1, User user2)
        {
            double score = 0;

            // Western Zodiac compatibility
            if (!string.IsNullOrEmpty(user1.ZodiacSign) && !string.IsNullOrEmpty(user2.ZodiacSign))
            {
                if (user1.ZodiacSign == user2.ZodiacSign)
                    score += 7; // Same sign
                else if (AreCompatibleZodiacSigns(user1.ZodiacSign, user2.ZodiacSign))
                    score += 5; // Compatible signs
            }

            // Hindu Nakshatra compatibility
            if (!string.IsNullOrEmpty(user1.Nakshatra) && !string.IsNullOrEmpty(user2.Nakshatra))
            {
                if (user1.Nakshatra == user2.Nakshatra)
                    score += 7;
                else if (AreCompatibleNakshatras(user1.Nakshatra, user2.Nakshatra))
                    score += 6;
            }

            // Chinese Zodiac compatibility
            if (!string.IsNullOrEmpty(user1.ChineseZodiac) && !string.IsNullOrEmpty(user2.ChineseZodiac))
            {
                if (AreCompatibleChineseZodiac(user1.ChineseZodiac, user2.ChineseZodiac))
                    score += 6;
            }

            return Math.Min(score, 20); // Max 20 points
        }

        // Simplified zodiac compatibility (you can enhance this)
        private bool AreCompatibleZodiacSigns(string sign1, string sign2)
        {
            var compatibilityMap = new Dictionary<string, List<string>>
            {
                { "Aries", new List<string> { "Leo", "Sagittarius", "Gemini", "Aquarius" } },
                { "Taurus", new List<string> { "Virgo", "Capricorn", "Cancer", "Pisces" } },
                { "Gemini", new List<string> { "Libra", "Aquarius", "Aries", "Leo" } },
                { "Cancer", new List<string> { "Scorpio", "Pisces", "Taurus", "Virgo" } },
                { "Leo", new List<string> { "Aries", "Sagittarius", "Gemini", "Libra" } },
                { "Virgo", new List<string> { "Taurus", "Capricorn", "Cancer", "Scorpio" } },
                { "Libra", new List<string> { "Gemini", "Aquarius", "Leo", "Sagittarius" } },
                { "Scorpio", new List<string> { "Cancer", "Pisces", "Virgo", "Capricorn" } },
                { "Sagittarius", new List<string> { "Aries", "Leo", "Libra", "Aquarius" } },
                { "Capricorn", new List<string> { "Taurus", "Virgo", "Scorpio", "Pisces" } },
                { "Aquarius", new List<string> { "Gemini", "Libra", "Aries", "Sagittarius" } },
                { "Pisces", new List<string> { "Cancer", "Scorpio", "Taurus", "Capricorn" } }
            };

            return compatibilityMap.ContainsKey(sign1) && compatibilityMap[sign1].Contains(sign2);
        }

        private bool AreCompatibleNakshatras(string nakshatra1, string nakshatra2)
        {
            // Simplified - in reality, Nakshatra compatibility is very complex
            // This is just a placeholder
            return nakshatra1 == nakshatra2;
        }

        private bool AreCompatibleChineseZodiac(string zodiac1, string zodiac2)
        {
            var compatibilityMap = new Dictionary<string, List<string>>
            {
                { "Rat", new List<string> { "Dragon", "Monkey", "Ox" } },
                { "Ox", new List<string> { "Rat", "Snake", "Rooster" } },
                { "Tiger", new List<string> { "Horse", "Dog", "Pig" } },
                { "Rabbit", new List<string> { "Goat", "Pig", "Dog" } },
                { "Dragon", new List<string> { "Rat", "Monkey", "Rooster" } },
                { "Snake", new List<string> { "Ox", "Rooster", "Monkey" } },
                { "Horse", new List<string> { "Tiger", "Goat", "Dog" } },
                { "Goat", new List<string> { "Rabbit", "Horse", "Pig" } },
                { "Monkey", new List<string> { "Rat", "Dragon", "Snake" } },
                { "Rooster", new List<string> { "Ox", "Snake", "Dragon" } },
                { "Dog", new List<string> { "Tiger", "Rabbit", "Horse" } },
                { "Pig", new List<string> { "Rabbit", "Goat", "Tiger" } }
            };

            return compatibilityMap.ContainsKey(zodiac1) && compatibilityMap[zodiac1].Contains(zodiac2);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
               // PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth ?? DateTime.MinValue,
                Age = user.Age ?? 0,
                Gender = user.Gender ?? string.Empty,
                MaxDistance = user.MaxDistance ?? 0,
                City = user.City,
                State = user.State,
                ProfilePhotos = JsonSerializer.Deserialize<List<string>>(user.ProfilePhotos) ?? new List<string>(),
                Hobbies = JsonSerializer.Deserialize<List<string>>(user.Hobbies) ?? new List<string>(),
                Interests = JsonSerializer.Deserialize<List<string>>(user.Interests) ?? new List<string>(),
                ZodiacSign = user.ZodiacSign ?? string.Empty,
                SunSign = user.SunSign ?? string.Empty,
                MoonSign = user.MoonSign ?? string.Empty,
                RashiSign = user.RashiSign ?? string.Empty,
                Nakshatra = user.Nakshatra ?? string.Empty,
                ChineseZodiac = user.ChineseZodiac ?? string.Empty,
                Bio = user.Bio,
                Occupation = user.Occupation,
                Education = user.Education,
                Height = user.Height,
                IsProfileComplete = user.IsProfileComplete,
                CreatedAt = user.CreatedAt
            };
        }
    }



}