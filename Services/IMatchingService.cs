using AuthAPI.Models;
using static AuthAPI.Services.MatchingService;

namespace AuthAPI.Services
{
    public interface IMatchingService
    {
        Task<MatchStats> GetMatchStatsAsync(int userId);

        Task<List<UserMatchDto>> GetPotentialMatchesAsync(int userId, int limit = 20);
        Task<SwipeResponse> SwipeAsync(int userId, SwipeRequest request);
        Task<List<UserDto>> GetMyMatchesAsync(int userId);
        Task<List<UserDto>> GetLikesReceivedAsync(int userId);
        Task<bool> UnmatchAsync(int userId, int matchedUserId);
    }
}