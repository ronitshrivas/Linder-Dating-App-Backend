using AuthAPI.Models;

namespace AuthAPI.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetCode);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendPasswordChangedEmailAsync(string toEmail, string userName);
    }
}