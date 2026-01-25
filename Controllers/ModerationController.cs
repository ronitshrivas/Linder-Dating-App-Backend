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
    public class ModerationController : ControllerBase
    {
        private readonly IModerationService _moderationService;

        public ModerationController(IModerationService moderationService)
        {
            _moderationService = moderationService;
        }

        // POST: api/moderation/report
        [HttpPost("report")]
        public async Task<IActionResult> ReportUser([FromBody] ReportUserRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _moderationService.ReportUserAsync(userId, request);

            if (success)
                return Ok(new { message = "User reported successfully" });

            return BadRequest(new { message = "Failed to report user" });
        }

        // POST: api/moderation/block
        [HttpPost("block")]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _moderationService.BlockUserAsync(userId, request.BlockedUserId);

            if (success)
                return Ok(new { message = "User blocked successfully" });

            return BadRequest(new { message = "Failed to block user" });
        }

        // DELETE: api/moderation/unblock/{blockedUserId}
        [HttpDelete("unblock/{blockedUserId}")]
        public async Task<IActionResult> UnblockUser(int blockedUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _moderationService.UnblockUserAsync(userId, blockedUserId);

            if (success)
                return Ok(new { message = "User unblocked successfully" });

            return BadRequest(new { message = "Failed to unblock user" });
        }

        // GET: api/moderation/blocked-users
        [HttpGet("blocked-users")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var blockedUsers = await _moderationService.GetBlockedUsersAsync(userId);
            return Ok(blockedUsers);
        }
    }
}