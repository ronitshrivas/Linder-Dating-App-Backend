using AuthAPI.Models;

namespace AuthAPI.Services
{
    public interface IPasswordService
    {
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<PasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<PasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<bool> ValidateResetCodeAsync(string email, string resetCode);
    }
}