using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ===== STEP 1: BASIC REGISTRATION =====
        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        // ===== STEP 2: COMPLETE PROFILE =====
        // POST: api/auth/complete-profile
        [Authorize] // Requires JWT token from Step 1
        [HttpPost("complete-profile")]
        public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _authService.CompleteProfileAsync(userId, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        // ===== GET PROFILE COMPLETION STATUS =====
        // GET: api/auth/profile-status
        [Authorize]
        [HttpGet("profile-status")]
        public async Task<IActionResult> GetProfileStatus()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var status = await _authService.GetProfileStatusAsync(userId);

            return Ok(status);
        }

        // ===== GET SIGNUP OPTIONS =====
        // GET: api/auth/signup-options
        [HttpGet("signup-options")]
        public IActionResult GetSignupOptions()
        {
            return Ok(new
            {
                genders = new[] { "Male", "Female", "Other", "Prefer not to say" },
                interestedInOptions = new[] { "Male", "Female", "Both" },
                minPhotos = 2,
                maxPhotos = 6,
                minAge = 18,
                maxAge = 100,
                minDistance = 1,
                maxDistance = 500,
                minHeight = 100,
                maxHeight = 250,
                hobbies = AppConstants.AvailableHobbies,
                interests = AppConstants.AvailableInterests,
                nakshatras = AppConstants.Nakshatras,
                rashiSigns = AppConstants.RashiSigns,
                zodiacSigns = AppConstants.ZodiacSigns,
                chineseZodiac = AppConstants.ChineseZodiacSigns
            });
        }

        // ===== LOGIN =====
        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }

            return Unauthorized(result);
        }

        // ===== GET PROFILE (Protected) =====
        // GET: api/auth/profile
        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var profileComplete = User.FindFirst("ProfileComplete")?.Value;

            return Ok(new
            {
                userId,
                email,
                name,
                profileComplete = bool.Parse(profileComplete ?? "false"),
                message = "This is a protected route!"
            });
        }

        // ===== TEST ENDPOINT =====
        // GET: api/auth/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "API is working!",
                timestamp = DateTime.UtcNow,
                version = "2.0 - Two-Step Registration"
            });
        }
    }
}