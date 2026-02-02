using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController : ControllerBase
    {
        private readonly IPasswordService _passwordService;

        public PasswordController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        // ===== FORGOT PASSWORD - SEND RESET CODE =====
        // POST: api/password/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _passwordService.ForgotPasswordAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        // ===== RESEND OTP CODE =====
        // POST: api/password/resend-code
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Reuse the same forgot password logic to generate and send new code
            var result = await _passwordService.ForgotPasswordAsync(request);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "A new reset code has been sent to your email. (Valid for 15 minutes)",
                    resetToken = result.ResetToken // Will be null in production
                });
            }

            return BadRequest(result);
        }

        // ===== VALIDATE RESET CODE + RESET PASSWORD (COMBINED) =====
        // POST: api/password/validate-reset-code
        [HttpPost("validate-resent-code")]
        public async Task<IActionResult> ValidateResetCode([FromBody] ValidateAndResetRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Convert to ResetPasswordRequest for the service
            var resetRequest = new ResetPasswordRequest
            {
                Email = request.Email,
                
                ResetCode = request.ResetCode,
                NewPassword = request.NewPassword
            };

            var result = await _passwordService.ResetPasswordAsync(resetRequest);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        // ===== CHANGE PASSWORD (LOGGED-IN USERS) =====
        // POST: api/password/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _passwordService.ChangePasswordAsync(userId, request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }

    // Request model for validate-reset-code endpoint
    public class ValidateAndResetRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reset code is required")]
        public string ResetCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }
}