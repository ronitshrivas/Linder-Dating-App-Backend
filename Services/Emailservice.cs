using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace AuthAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ===== SEND PASSWORD RESET EMAIL =====
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetCode)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:FromName"] ?? "Linder Dating App",
                    _configuration["EmailSettings:FromEmail"]
                ));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = "Password Reset Code - Linder Dating App";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                          color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                                .code {{ font-size: 32px; font-weight: bold; color: #667eea; 
                                        text-align: center; padding: 20px; background: white; 
                                        border-radius: 10px; margin: 20px 0; letter-spacing: 5px; }}
                                .warning {{ color: #e74c3c; font-weight: bold; margin-top: 20px; }}
                                .footer {{ text-align: center; margin-top: 30px; color: #777; font-size: 12px; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>🔐 Password Reset Request</h1>
                                </div>
                                <div class='content'>
                                    <p>Hi there,</p>
                                    <p>You requested to reset your password for your Linder Dating App account.</p>
                                    <p>Your password reset code is:</p>
                                    <div class='code'>{resetCode}</div>
                                    <p>This code will <strong>expire in 15 minutes</strong>.</p>
                                    <p>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
                                    <div class='warning'>
                                        ⚠️ Never share this code with anyone. Linder team will never ask for your reset code.
                                    </div>
                                </div>
                                <div class='footer'>
                                    <p>This is an automated email from Linder Dating App.</p>
                                    <p>© 2026 Linder. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>
                    ",
                    TextBody = $@"
                        Password Reset Request
                        
                        Hi there,
                        
                        You requested to reset your password for your Linder Dating App account.
                        
                        Your password reset code is: {resetCode}
                        
                        This code will expire in 15 minutes.
                        
                        If you didn't request this password reset, please ignore this email.
                        
                        Never share this code with anyone.
                        
                        © 2026 Linder Dating App
                    "
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        _configuration["EmailSettings:SmtpServer"],
                        int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
                        SecureSocketOptions.StartTls
                    );

                    await client.AuthenticateAsync(
                        _configuration["EmailSettings:Username"],
                        _configuration["EmailSettings:Password"]
                    );

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }

        // ===== SEND WELCOME EMAIL =====
        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:FromName"] ?? "Linder Dating App",
                    _configuration["EmailSettings:FromEmail"]
                ));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Welcome to Linder Dating App! 💕";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                          color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>Welcome to Linder! 💕</h1>
                                </div>
                                <div class='content'>
                                    <h2>Hi {userName},</h2>
                                    <p>Welcome to Linder Dating App! We're excited to have you on board.</p>
                                    <p>Start by completing your profile to get better matches!</p>
                                    <p>Happy matching!</p>
                                    <p>Best regards,<br>The Linder Team</p>
                                </div>
                            </div>
                        </body>
                        </html>
                    "
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        _configuration["EmailSettings:SmtpServer"],
                        int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
                        SecureSocketOptions.StartTls
                    );

                    await client.AuthenticateAsync(
                        _configuration["EmailSettings:Username"],
                        _configuration["EmailSettings:Password"]
                    );

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }

        // ===== SEND PASSWORD CHANGED NOTIFICATION =====
        public async Task<bool> SendPasswordChangedEmailAsync(string toEmail, string userName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:FromName"] ?? "Linder Dating App",
                    _configuration["EmailSettings:FromEmail"]
                ));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Password Changed - Linder Dating App";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                          color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                                .warning {{ color: #e74c3c; font-weight: bold; margin-top: 20px; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>🔒 Password Changed</h1>
                                </div>
                                <div class='content'>
                                    <p>Hi {userName},</p>
                                    <p>This is to confirm that your password was successfully changed on {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC.</p>
                                    <div class='warning'>
                                        ⚠️ If you didn't make this change, please contact support immediately.
                                    </div>
                                    <p>Best regards,<br>The Linder Team</p>
                                </div>
                            </div>
                        </body>
                        </html>
                    "
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        _configuration["EmailSettings:SmtpServer"],
                        int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
                        SecureSocketOptions.StartTls
                    );

                    await client.AuthenticateAsync(
                        _configuration["EmailSettings:Username"],
                        _configuration["EmailSettings:Password"]
                    );

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
                return false;
            }
        }
    }
}