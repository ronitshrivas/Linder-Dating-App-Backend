using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;

        public MatchingController(IMatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        // GET: api/matching/discover?limit=20
        [HttpGet("discover")]
        public async Task<IActionResult> GetPotentialMatches([FromQuery] int limit = 20)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var matches = await _matchingService.GetPotentialMatchesAsync(userId, limit);
            return Ok(matches);
        }

        // POST: api/matching/swipe
        [HttpPost("swipe")]
        public async Task<IActionResult> Swipe([FromBody] SwipeRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _matchingService.SwipeAsync(userId, request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        // GET: api/matching/my-matches
        [HttpGet("my-matches")]
        public async Task<IActionResult> GetMyMatches()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var matches = await _matchingService.GetMyMatchesAsync(userId);
            return Ok(matches);
        }

        // GET: api/matching/likes-received
        [HttpGet("likes-received")]
        public async Task<IActionResult> GetLikesReceived()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var likes = await _matchingService.GetLikesReceivedAsync(userId);
            return Ok(likes);
        }

        // DELETE: api/matching/unmatch/{matchedUserId}
        [HttpDelete("unmatch/{matchedUserId}")]
        public async Task<IActionResult> Unmatch(int matchedUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _matchingService.UnmatchAsync(userId, matchedUserId);

            if (success)
                return Ok(new { message = "Unmatched successfully" });

            return BadRequest(new { message = "Failed to unmatch" });
        }

        // GET: api/matching/match-stats
        [HttpGet("match-stats")]
        public async Task<IActionResult> GetMatchStats()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var stats = await _matchingService.GetMatchStatsAsync(userId);
            return Ok(stats);
        }
    }
}