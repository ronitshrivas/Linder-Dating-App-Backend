using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]  // Routes will be: api/auth/signup, api/auth/login
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // Constructor - dependency injection brings in AuthService
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Call service to register user
            var result = await _authService.SignupAsync(request);

            // Return response based on success/failure
            if (result.Success)
            {
                return Ok(result);  // 200 OK
            }

            return BadRequest(result);  // 400 Bad Request
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Call service to login user
            var result = await _authService.LoginAsync(request);

            // Return response based on success/failure
            if (result.Success)
            {
                return Ok(result);  // 200 OK
            }

            return Unauthorized(result);  // 401 Unauthorized
        }

        // GET: api/auth/profile (Protected route example)
        [Authorize]  // This requires a valid JWT token
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            // Get user info from JWT token claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new
            {
                userId,
                email,
                name,
                message = "This is a protected route!"
            });
        }

        // GET: api/auth/test (Public route example)
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "API is working!",
                timestamp = DateTime.UtcNow
            });
        }
    }
}