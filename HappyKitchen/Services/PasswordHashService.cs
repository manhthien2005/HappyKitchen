using BCrypt.Net;

namespace HappyKitchen.Services // Đổi thành namespace của bạn
{
    public static class PasswordService
    {
        // Hash password
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Kiểm tra mật khẩu có khớp với hash không
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
