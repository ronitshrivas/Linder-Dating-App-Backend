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
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        // GET: api/profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var profile = await _profileService.GetProfileAsync(userId);

            if (profile == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(profile);
        }

        // GET: api/profile/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            var profile = await _profileService.GetProfileAsync(userId);

            if (profile == null)
                return NotFound(new { message = "Profile not found" });

            return Ok(profile);
        }

        // PUT: api/profile/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _profileService.UpdateProfileAsync(userId, request);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        // DELETE: api/profile/delete-account
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _profileService.DeleteAccountAsync(userId);

            if (success)
                return Ok(new { message = "Account deleted successfully" });

            return BadRequest(new { message = "Failed to delete account" });
        }
    }
}