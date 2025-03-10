using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace HappyKitchen.Services
{
    public class EmailService
    {
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly string _smtpServer;
        private readonly int _smtpPort;

        public EmailService(IConfiguration configuration)
        {
            _fromEmail = configuration["EmailSettings:SenderEmail"];
            _password = configuration["EmailSettings:SenderPassword"];
            _smtpServer = "smtp.gmail.com"; // SMTP server của Gmail
            _smtpPort = 587; // Cổng SMTP của Gmail
        }

        private Task SendEmailAsync(string toEmail, string subject, string body)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using var smtpClient = new SmtpClient(_smtpServer)
                    {
                        Port = _smtpPort,
                        Credentials = new NetworkCredential(_fromEmail, _password),
                        EnableSsl = true,
                        Timeout = 10000 // 10 giây
                    };

                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                }
            });
        }

        public void SendOTP(string toEmail, string otpCode)
        {
            string subject = "Mã OTP xác thực đăng ký";
            string body = $"Mã OTP của bạn là: <b>{otpCode}</b>";
            SendEmailAsync(toEmail, subject, body);
        }

        public void SendResetPasswordOTP(string toEmail, string otpCode)
        {
            string subject = "🔐 Password Reset OTP";
            string body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 5px; max-width: 500px; margin: auto;'>
                <h2 style='color: #333;'>🔐 Reset Your Password</h2>
                <p>We received a request to reset your password. Use the OTP code below to proceed:</p>
                <div style='background: #f4f4f4; padding: 10px; border-radius: 5px; text-align: center; font-size: 20px; font-weight: bold;'>
                    {otpCode}
                </div>
                <p>This OTP is valid for <strong>5 minutes</strong>. If you did not request this, please ignore this email.</p>
                <p>Thank you,<br><strong>Your Website Team</strong></p>
            </div>";
            SendEmailAsync(toEmail, subject, body);
        }

        public void SendLoginOTP(string toEmail, string otpCode)
        {
            string subject = "🔑 New Device Login OTP";
            string body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 5px; max-width: 500px; margin: auto;'>
                <h2 style='color: #333;'>🔑 Verify Your Login</h2>
                <p>We detected a login attempt from a new device. Please enter the OTP below to verify your identity:</p>
                <div style='background: #f4f4f4; padding: 10px; border-radius: 5px; text-align: center; font-size: 20px; font-weight: bold;'>
                    {otpCode}
                </div>
                <p>This OTP is valid for <strong>5 minutes</strong>. If this wasn't you, please secure your account immediately.</p>
                <p>Thank you,<br><strong>Your Website Team</strong></p>
            </div>";
            SendEmailAsync(toEmail, subject, body);
        }

    }
}
