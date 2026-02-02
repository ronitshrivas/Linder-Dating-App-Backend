using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuthAPI.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly AppDbContext _context;

        public MatchingService(AppDbContext context)
        {
            _context = context;
        }

        // ===== GET POTENTIAL MATCHES =====
        public async Task<List<UserMatchDto>> GetPotentialMatchesAsync(int userId, int limit = 20)
        {
            try
            {
                var currentUser = await _context.Users.FindAsync(userId);
                if (currentUser == null)
                    return new List<UserMatchDto>();

                // Get users already swiped on
                var swipedUserIds = await _context.Matches
                    .Where(m => m.UserId == userId)
                    .Select(m => m.TargetUserId)
                    .ToListAsync();

                // Get blocked users (both ways)
                var blockedUserIds = await _context.UserBlocks
                    .Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                    .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId)
                    .ToListAsync();

                // Combine excluded users
                var excludedUserIds = swipedUserIds.Union(blockedUserIds).ToList();

                // Get potential matches (exclude already swiped and blocked users)
                var potentialUsers = await _context.Users
                    .Where(u => u.Id != userId && !excludedUserIds.Contains(u.Id))
                    .Take(100) // Get more than needed for scoring
                    .ToListAsync();

                // If no users found, return empty list
                if (!potentialUsers.Any())
                    return new List<UserMatchDto>();

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
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error in GetPotentialMatchesAsync: {ex.Message}");
                return new List<UserMatchDto>();
            }
        }

        // ===== SWIPE ON USER =====
        public async Task<SwipeResponse> SwipeAsync(int userId, SwipeRequest request)
        {
            try
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

                // If it's a LIKE or SUPERLIKE, check if there's a mutual match
                if (request.Action == SwipeAction.Like || request.Action == SwipeAction.SuperLike)
                {
                    var reverseMatch = await _context.Matches
                        .FirstOrDefaultAsync(m =>
                            m.UserId == request.TargetUserId &&
                            m.TargetUserId == userId &&
                            (m.Action == SwipeAction.Like || m.Action == SwipeAction.SuperLike));

                    if (reverseMatch != null)
                    {
                        // It's a match!
                        match.IsMatch = true;
                        match.MatchedAt = DateTime.UtcNow;
                        reverseMatch.IsMatch = true;
                        reverseMatch.MatchedAt = DateTime.UtcNow;

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
                    Message = request.Action == SwipeAction.Like ? "Liked!" :
                             request.Action == SwipeAction.SuperLike ? "Super Liked!" : "Passed",
                    IsMatch = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SwipeAsync: {ex.Message}");
                return new SwipeResponse
                {
                    Success = false,
                    Message = "Failed to process swipe"
                };
            }
        }

        // ===== GET MY MATCHES =====
        public async Task<List<UserDto>> GetMyMatchesAsync(int userId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMyMatchesAsync: {ex.Message}");
                return new List<UserDto>();
            }
        }

        // ===== GET LIKES RECEIVED =====
        public async Task<List<UserDto>> GetLikesReceivedAsync(int userId)
        {
            try
            {
                // Get users who liked me (but I haven't swiped on yet)
                var likedMeUserIds = await _context.Matches
                    .Where(m => m.TargetUserId == userId &&
                               (m.Action == SwipeAction.Like || m.Action == SwipeAction.SuperLike) &&
                               !m.IsMatch)
                    .Select(m => m.UserId)
                    .ToListAsync();

                var usersWhoLikedMe = await _context.Users
                    .Where(u => likedMeUserIds.Contains(u.Id))
                    .ToListAsync();

                return usersWhoLikedMe.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLikesReceivedAsync: {ex.Message}");
                return new List<UserDto>();
            }
        }

        // ===== UNMATCH =====
        public async Task<bool> UnmatchAsync(int userId, int matchedUserId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UnmatchAsync: {ex.Message}");
                return false;
            }
        }

        // ===== GET MATCH STATS =====
        public async Task<MatchStats> GetMatchStatsAsync(int userId)
        {
            try
            {
                var totalMatches = await _context.Matches
                    .CountAsync(m => m.UserId == userId && m.IsMatch);

                var totalLikes = await _context.Matches
                    .CountAsync(m => m.UserId == userId &&
                               (m.Action == SwipeAction.Like || m.Action == SwipeAction.SuperLike));

                var totalPasses = await _context.Matches
                    .CountAsync(m => m.UserId == userId && m.Action == SwipeAction.Pass);

                var likesReceived = await _context.Matches
                    .CountAsync(m => m.TargetUserId == userId &&
                               (m.Action == SwipeAction.Like || m.Action == SwipeAction.SuperLike));

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMatchStatsAsync: {ex.Message}");
                return new MatchStats
                {
                    TotalMatches = 0,
                    TotalLikes = 0,
                    TotalPasses = 0,
                    LikesReceived = 0,
                    SuperLikes = 0,
                    MatchRate = 0
                };
            }
        }

        // ===== CALCULATE MATCH SCORE (NULL-SAFE) =====
        private double CalculateMatchScore(User currentUser, User targetUser, out MatchBreakdown breakdown)
        {
            breakdown = new MatchBreakdown();

            try
            {
                // Parse JSON arrays safely
                var currentHobbies = SafeDeserializeList(currentUser.Hobbies);
                var targetHobbies = SafeDeserializeList(targetUser.Hobbies);
                var currentInterests = SafeDeserializeList(currentUser.Interests);
                var targetInterests = SafeDeserializeList(targetUser.Interests);

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

                // 4. Age Compatibility (15%) - NULL-SAFE
                var ageDiff = Math.Abs((currentUser.Age ?? 0) - (targetUser.Age ?? 0));
                breakdown.AgeCompatibility = ageDiff <= 5 ? 15 :
                                             ageDiff <= 10 ? 10 :
                                             ageDiff <= 15 ? 5 : 0;

                // 5. Distance Score (10%) - assuming same city for now
                breakdown.DistanceScore = (currentUser.City ?? "") == (targetUser.City ?? "") ? 10 : 5;

                // Total score
                var totalScore = breakdown.InterestScore +
                               breakdown.HobbyScore +
                               breakdown.HoroscopeScore +
                               breakdown.AgeCompatibility +
                               breakdown.DistanceScore;

                return Math.Round(totalScore, 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating match score: {ex.Message}");
                return 0;
            }
        }

        // ===== SAFE JSON DESERIALIZATION =====
        private List<string> SafeDeserializeList(string? json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        // ===== HOROSCOPE COMPATIBILITY =====
        private double CalculateHoroscopeCompatibility(User user1, User user2)
        {
            double score = 0;

            // Western Zodiac compatibility
            if (!string.IsNullOrEmpty(user1.ZodiacSign) && !string.IsNullOrEmpty(user2.ZodiacSign))
            {
                if (user1.ZodiacSign == user2.ZodiacSign)
                    score += 7;
                else if (AreCompatibleZodiacSigns(user1.ZodiacSign, user2.ZodiacSign))
                    score += 5;
            }

            // Hindu Nakshatra compatibility
            if (!string.IsNullOrEmpty(user1.Nakshatra) && !string.IsNullOrEmpty(user2.Nakshatra))
            {
                if (user1.Nakshatra == user2.Nakshatra)
                    score += 7;
            }

            // Chinese Zodiac compatibility
            if (!string.IsNullOrEmpty(user1.ChineseZodiac) && !string.IsNullOrEmpty(user2.ChineseZodiac))
            {
                if (AreCompatibleChineseZodiac(user1.ChineseZodiac, user2.ChineseZodiac))
                    score += 6;
            }

            return Math.Min(score, 20);
        }

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

        // ===== MAP TO DTO (NULL-SAFE) =====
        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = string.Empty,
                DateOfBirth = user.DateOfBirth ?? DateTime.MinValue,
                Age = user.Age ?? 0,
                Gender = user.Gender ?? string.Empty,
                MaxDistance = user.MaxDistance ?? 0,
                City = user.City,
                State = user.State,
                ProfilePhotos = SafeDeserializeList(user.ProfilePhotos),
                Hobbies = SafeDeserializeList(user.Hobbies),
                Interests = SafeDeserializeList(user.Interests),
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

        public class MatchStats
        {
            public int TotalMatches { get; set; }
            public int TotalLikes { get; set; }
            public int TotalPasses { get; set; }
            public int LikesReceived { get; set; }
            public int SuperLikes { get; set; }
            public double MatchRate { get; set; }
        }
    }
}