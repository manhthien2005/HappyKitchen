using System;
using System.Net;
using System.Net.Mail;

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
            string subject = "🎉 Chào mừng! Mã OTP đăng ký của bạn";
            string body = GetEmailTemplate(
                "🎉 Xác nhận đăng ký của bạn",
                $"<p>Cảm ơn bạn đã đăng ký! Để hoàn tất đăng ký, vui lòng sử dụng mã OTP bên dưới:</p>" +
                $"<div style='background: #e6f7e6; padding: 10px; border-radius: 5px; text-align: center; font-size: 20px; font-weight: bold; color: #2e7d32;'>{otpCode}</div>" +
                "<p>Mã OTP này có hiệu lực trong <strong>5 phút</strong>.</p>" +
                "<p>Chúng tôi rất vui được chào đón bạn!<br><strong>Thân ái, Happy Kitchen.</strong></p>"
            );
            SendEmailAsync(toEmail, subject, body);
        }

        public void SendResetPasswordOTP(string toEmail, string otpCode)
        {
            string subject = "🔐 Đặt lại mật khẩu của bạn";
            string body = GetEmailTemplate(
                "🔐 Yêu cầu đặt lại mật khẩu",
                $"<p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu của bạn. Vui lòng sử dụng mã OTP bên dưới để tiếp tục:</p>" +
                $"<div style='background: #ffe0b2; padding: 10px; border-radius: 5px; text-align: center; font-size: 20px; font-weight: bold; color: #e65100;'>{otpCode}</div>" +
                "<p>Mã OTP này có hiệu lực trong <strong>5 phút</strong>.</p>" +
                "<p>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này.<br><strong>Thân ái, Happy Kitchen.</strong></p>"
            );
            SendEmailAsync(toEmail, subject, body);
        }

        public void SendLoginOTP(string toEmail, string otpCode)
        {
            string subject = "🔑 Xác minh đăng nhập của bạn";
            string body = GetEmailTemplate(
                "🔑 Phát hiện đăng nhập từ thiết bị mới",
                $"<p>Chúng tôi đã phát hiện một lần đăng nhập từ thiết bị mới. Vui lòng nhập mã OTP bên dưới để xác minh danh tính của bạn:</p>" +
                $"<div style='background: #e3f2fd; padding: 10px; border-radius: 5px; text-align: center; font-size: 20px; font-weight: bold; color: #1565c0;'>{otpCode}</div>" +
                "<p>Mã OTP này có hiệu lực trong <strong>5 phút</strong>.</p>" +
                "<p>Nếu đây không phải là bạn, vui lòng bảo mật tài khoản của bạn ngay lập tức.<br><strong>Thân ái, Happy Kitchen.</strong></p>"
            );
            SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailVerificationOTP(string toEmail, string otpCode)
        {
            string subject = "📧 Xác thực email của bạn";
            string body = GetEmailTemplate(
                "📧 Xác thực địa chỉ email",
                $"<p>Chúng tôi đã nhận được yêu cầu xác thực email của bạn. Vui lòng sử dụng mã OTP bên dưới để xác thực địa chỉ email của bạn:</p>" +
                $"<div style='background: #e8f5e9; padding: 10px; border-radius: 5px; text-align: center; font-size: 20px; font-weight: bold; color: #2e7d32;'>{otpCode}</div>" +
                "<p>Mã OTP này có hiệu lực trong <strong>5 phút</strong>.</p>" +
                "<p>Nếu bạn không yêu cầu xác thực email này, vui lòng bỏ qua email này.<br><strong>Thân ái, Happy Kitchen.</strong></p>"
            );
            await SendEmailAsync(toEmail, subject, body);
        }

        private string GetEmailTemplate(string title, string content)
        {
            return $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background-color: #f4f4f4;
                        margin: 0;
                        padding: 0;
                    }}
                    .email-container {{
                        max-width: 600px;
                        margin: 20px auto;
                        background-color: #ffffff;
                        border: 1px solid #ddd;
                        border-radius: 5px;
                        overflow: hidden;
                    }}
                    .header {{
                        background-color: hsla(0, 0%, 13%, 1);
                        padding: 20px;
                        text-align: center;
                    }}
                    .header img {{
                        max-width: 150px;
                        height: auto;
                    }}
                    .content {{
                        padding: 20px;
                        color: #333;
                    }}
                    .content h2 {{
                        color: #8B4513;
                    }}
                    .content p {{
                        line-height: 1.6;
                    }}
                    .footer {{
                        background-color: hsla(0, 0%, 13%, 1);
                        padding: 10px;
                        text-align: center;
                        color: #ffffff;
                        font-size: 12px;
                    }}
                    .footer a {{
                        color: #ffffff;
                        text-decoration: none;
                        margin: 0 5px;
                    }}
                    .social-icons img {{
                        width: 24px;
                        height: 24px;
                        margin: 0 5px;
                    }}
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <!-- Phần đầu: Logo -->
                    <div class='header'>
                        <img src='https://i.imgur.com/MyazyH9.png' alt='Logo Nhà Hàng Gia Đình'>
                    </div>
                    
                    <!-- Phần nội dung chính -->
                    <div class='content'>
                        <h2>{title}</h2>
                        {content}
                    </div>
                    
                    <!-- Phần chân trang: Thông tin liên hệ và mạng xã hội -->
                    <div class='footer'>
                        <p>Happy Kitchen | Địa chỉ: 01 Thảo Điền, Hồ Chí Minh, Việt Nam | Điện thoại: 0977 139 203 | Email: happykitchenvn2025@gmail.com</p>
                        <div class='social-icons'>
                            <a href='https://facebook.com/nhahanggiadinh'><img width=""48"" height=""48"" src=""https://img.icons8.com/color/48/facebook-new.png"" alt=""facebook-new""/></a>
                            <a href='https://instagram.com/nhahanggiadinh'><img width=""48"" height=""48"" src=""https://img.icons8.com/fluency/48/instagram-new.png"" alt=""instagram-new""/></a>
                            <a href='https://twitter.com/nhahanggiadinh'><img width=""48"" height=""48"" src=""https://img.icons8.com/ios-filled/50/twitterx--v1.png"" alt=""twitterx--v1""/></a>
                        </div>
                        <p>© 2025 Happy Kitchen. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}