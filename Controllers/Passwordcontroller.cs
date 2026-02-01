using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // ===== RESET PASSWORD - USE RESET CODE =====
        // POST: api/password/reset-password
        //[HttpPost("reset-password")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var result = await _passwordService.ResetPasswordAsync(request);

        //    if (result.Success)
        //    {
        //        return Ok(result);
        //    }

        //    return BadRequest(result);
        //}

        
        // POST: api/password/validate-reset-code
        [HttpPost("validate-resent-code")]
        public async Task<IActionResult> ValidateResetCode([FromBody] ValidateResetCodeRequest request)
        {
            var isValid = await _passwordService.ValidateResetCodeAsync(request.Email, request.ResetCode);

            if (isValid)
            {
                return Ok(new { valid = true, message = "Reset code is valid" });
            }

            return BadRequest(new { valid = false, message = "Invalid or expired reset code" });
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

    // Request model for validating reset code
    public class ValidateResetCodeRequest
    {
        public string Email { get; set; } = string.Empty;
        public string ResetCode { get; set; } = string.Empty;
    }
}