using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthAPI.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public PasswordService(AppDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        // ===== FORGOT PASSWORD - GENERATE RESET CODE =====
        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // For security, don't reveal if email exists or not
            if (user == null)
            {
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If this email exists, a password reset code has been sent to it."
                };
            }

            // Invalidate any existing reset codes for this user
            var existingResets = await _context.PasswordResets
                .Where(pr => pr.UserId == user.Id && !pr.IsUsed)
                .ToListAsync();

            foreach (var reset in existingResets)
            {
                reset.IsUsed = true;
            }

            // Generate 6-digit reset code
            var resetCode = GenerateResetCode();

            // Create password reset record
            var passwordReset = new PasswordReset
            {
                UserId = user.Id,
                ResetCode = resetCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Valid for 15 minutes
                IsUsed = false
            };

            _context.PasswordResets.Add(passwordReset);
            await _context.SaveChangesAsync();

            // ===== SEND EMAIL WITH RESET CODE =====
            var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, resetCode);

            // Check if we're in development mode
            var isDevelopment = _configuration.GetValue<bool>("EmailSettings:IsDevelopment", true);

            if (emailSent)
            {
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Password reset code has been sent to your email. (Valid for 15 minutes)",
                    ResetToken = isDevelopment ? resetCode : null // Only show code in development
                };
            }
            else
            {
                // Email failed to send
                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If this email exists, a password reset code has been sent to it.",
                    ResetToken = isDevelopment ? resetCode : null // Show code in dev even if email fails
                };
            }
        }

        // ===== RESET PASSWORD USING CODE =====
        public async Task<PasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = "Invalid email or reset code"
                };
            }

            // Find valid reset code
            var resetRecord = await _context.PasswordResets
                .Where(pr => pr.UserId == user.Id &&
                            pr.ResetCode == request.ResetCode &&
                            !pr.IsUsed &&
                            pr.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(pr => pr.CreatedAt)
                .FirstOrDefaultAsync();

            if (resetRecord == null)
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = "Invalid or expired reset code"
                };
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastActive = DateTime.UtcNow;

            // Mark reset code as used
            resetRecord.IsUsed = true;

            await _context.SaveChangesAsync();

            // ===== SEND PASSWORD CHANGED NOTIFICATION =====
            await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FullName);

            return new PasswordResponse
            {
                Success = true,
                Message = "Password reset successfully. You can now login with your new password."
            };
        }

        // ===== CHANGE PASSWORD (FOR LOGGED-IN USERS) =====
        public async Task<PasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Verify current password
            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);

            if (!isCurrentPasswordValid)
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = "Current password is incorrect"
                };
            }

            // Check if new password is same as current password
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            {
                return new PasswordResponse
                {
                    Success = false,
                    Message = "New password must be different from current password"
                };
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastActive = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ===== SEND PASSWORD CHANGED NOTIFICATION =====
            await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FullName);

            return new PasswordResponse
            {
                Success = true,
                Message = "Password changed successfully"
            };
        }

        // ===== VALIDATE RESET CODE (OPTIONAL - FOR CHECKING BEFORE RESET) =====
        public async Task<bool> ValidateResetCodeAsync(string email, string resetCode)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return false;

            var resetRecord = await _context.PasswordResets
                .Where(pr => pr.UserId == user.Id &&
                            pr.ResetCode == resetCode &&
                            !pr.IsUsed &&
                            pr.ExpiresAt > DateTime.UtcNow)
                .AnyAsync();

            return resetRecord;
        }

        // ===== HELPER METHOD - GENERATE 6-DIGIT CODE =====
        private string GenerateResetCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}