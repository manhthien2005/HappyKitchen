using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;
using static System.Net.WebRequestMethods;

namespace HappyKitchen.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Menu()
        {
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.Status == 1)) // Chỉ lấy các món còn hàng
                .ToListAsync();

            return View(categories);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] EmployeeLogin model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.RecaptchaToken))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            // Kiểm tra reCAPTCHA
            bool isRecaptchaValid = await VerifyRecaptcha(model.RecaptchaToken);
            if (!isRecaptchaValid)
            {
                return Json(new { success = false, message = "Xác thực reCAPTCHA thất bại." });
            }

            // Kiểm tra tài khoản
            var user = _context.Employees.AsNoTracking().FirstOrDefault(e => e.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });
            }

            return Json(new { success = true, message = "Đăng nhập thành công!" });
        }

        // Hàm xác minh reCAPTCHA
        private async Task<bool> VerifyRecaptcha(string recaptchaToken)
        {
            string secretKey = "6Le7Le8qAAAAAFfAqwHWHvPrVToCuSXafwzobYgV";  // 🔹 Thay bằng Secret Key của bạn từ Google reCAPTCHA
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaToken}", null);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                return result.success == "true";
            }
        }

        public IActionResult Verify_Login()
        {
            return View();
        }



        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(EmployeeRegister model)
        {
            if (ModelState.IsValid)
            {
                bool emailExists = await _context.Employees.AnyAsync(e => e.Email == model.Email);
                bool phoneExists = await _context.Employees.AnyAsync(e => e.PhoneNumber == model.PhoneNumber);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại.");
                }

                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại đã tồn tại.");
                }

                if (emailExists || phoneExists)
                {
                    return View(model);
                }

                // Lưu thông tin vào Session
                HttpContext.Session.SetString("FullName", model.FullName);
                HttpContext.Session.SetString("Email", model.Email);
                HttpContext.Session.SetString("Password", model.Password);
                HttpContext.Session.SetString("PhoneNumber", model.PhoneNumber);

                // Tạo và lưu OTP vào Session
                string otpCode = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("OTP_SignUp", otpCode);
                HttpContext.Session.SetString("OTP_SignUp_Timestamp", DateTime.Now.ToString());

                // Gửi email OTP
                var emailService = new EmailService(_configuration);
                emailService.SendOTP(model.Email, otpCode);

                return RedirectToAction("VerifyOTP", new { email = model.Email });
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult VerifyOTP(string email)
        {
            string sessionEmail = HttpContext.Session.GetString("Email");
            string sessionOTP = HttpContext.Session.GetString("OTP_SignUp");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sessionEmail) || string.IsNullOrEmpty(sessionOTP) || sessionEmail != email)
            {
                return RedirectToAction("SignUp");
            }

            return View("VerifyOTP", email);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTPCheck([FromBody] OTPModel model)
        {   
            string sessionOTP = HttpContext.Session.GetString("OTP_SignUp");
            string fullName = HttpContext.Session.GetString("FullName");
            string email = HttpContext.Session.GetString("Email");
            string password = HttpContext.Session.GetString("Password");
            string phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            string otpTimestamp = HttpContext.Session.GetString("OTP_SignUp_Timestamp");
            int failedAttempts = HttpContext.Session.GetInt32("FailedOTP_SignUp_Attempts") ?? 0;

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn, vui lòng thử lại." });
            }

            // Kiểm tra thời gian hết hạn OTP (5 phút)
            if (DateTime.TryParse(otpTimestamp, out DateTime otpTime) && (DateTime.Now - otpTime).TotalMinutes > 3)
            {
                HttpContext.Session.Remove("OTP_SignUp");
                HttpContext.Session.Remove("OTP_SignUp_Timestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng thử lại." });
            }

            // Kiểm tra số lần nhập sai OTP
            if (failedAttempts >= 3)
            {
                HttpContext.Session.Remove("OTP_SignUp");
                HttpContext.Session.Remove("OTP_SignUp_Timestamp");
                return Json(new { success = false, message = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã mới." });
            }

            if (model.OTPCode != sessionOTP)
            {
                HttpContext.Session.SetInt32("FailedOTP_SignUp_Attempts", failedAttempts + 1);
                return Json(new { success = false, message = "Mã OTP không chính xác. Vui lòng thử lại." });
            }

            // Xóa OTP khỏi session sau khi xác thực thành công
            HttpContext.Session.Remove("OTP_SignUp");
            HttpContext.Session.Remove("OTP_SignUp_Timestamp");
            HttpContext.Session.Remove("FailedOTP_SignUp_Attempts");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Lưu nhân viên vào database
            var newEmployee = new Employee
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = hashedPassword
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            return Json(new { success = true, redirectUrl = Url.Action("Login", "Home") });
        }

        [HttpPost]
        public async Task<IActionResult> ResendOTP([FromBody] string email)
        {
            // Kiểm tra xem email có khớp với email đang đăng ký trong Session không
            string sessionEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn hoặc email không hợp lệ!" });
            }

            // Tạo OTP mới
            string newOtp = new Random().Next(100000, 999999).ToString();

            // Cập nhật OTP và thời gian vào Session
            HttpContext.Session.SetString("OTP_SignUp", newOtp);
            HttpContext.Session.SetString("OTP_SignUp_Timestamp", DateTime.Now.ToString());

            // Đặt lại số lần nhập sai OTP
            HttpContext.Session.SetInt32("FailedOTP_SignUp_Attempts", 0);

            // Gửi OTP qua email
            var emailService = new EmailService(_configuration);
            emailService.SendOTP(email, newOtp);

            return Json(new { success = true, message = "OTP mới đã được gửi!" });
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public IActionResult SendPasswordOTP([FromBody] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email không hợp lệ!" });
            }

            // Tạo OTP 6 chữ số
            string otpCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("OTP_ResetPass", otpCode);
            HttpContext.Session.SetString("OTP_ResetPass_Timestamp", DateTime.Now.ToString());

            // Gửi email OTP
            var emailService = new EmailService(_configuration);
            emailService.SendResetPasswordOTP(email, otpCode);

            HttpContext.Session.SetString("ForgotPass_Email", email);

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult VerifyPasswordOTP(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            return View("VerifyPasswordOTP", email);
        }

        [HttpPost] // CHUYỂN TỪ GET -> POST
        public IActionResult VerifyPasswordOTP([FromBody] OTPPasswordModel model)
        {
            string sessionOTP = HttpContext.Session.GetString("OTP_ResetPass");
            string email = HttpContext.Session.GetString("ForgotPass_Email");
            string otpTimestamp = HttpContext.Session.GetString("OTP_ResetPass_Timestamp");
            int failedAttempts = HttpContext.Session.GetInt32("FailedOTP_ResetPass_Attempts") ?? 0;

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn, vui lòng thử lại." });
            }

            // Kiểm tra thời gian hết hạn OTP (5 phút)
            if (DateTime.TryParse(otpTimestamp, out DateTime otpTime) && (DateTime.Now - otpTime).TotalMinutes > 5)
            {
                HttpContext.Session.Remove("OTP_ResetPass");
                HttpContext.Session.Remove("OTP_ResetPass_Timestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng thử lại." });
            }

            // Kiểm tra số lần nhập sai OTP
            if (failedAttempts >= 3)
            {
                HttpContext.Session.Remove("OTP_ResetPass");
                HttpContext.Session.Remove("OTP_ResetPass_Timestamp");
                return Json(new { success = false, message = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã mới." });
            }

            if (model.OTPPassCode != sessionOTP)
            {
                HttpContext.Session.SetInt32("FailedOTP_ResetPass_Attempts", failedAttempts + 1);
                return Json(new { success = false, message = "Mã OTP không chính xác. Vui lòng thử lại." });
            }

            // Nếu OTP hợp lệ, xóa OTP khỏi session và chuyển đến trang đặt lại mật khẩu
            HttpContext.Session.SetString("OTP_ResetPass_Verified", "true");
            HttpContext.Session.SetString("OTPVerified_ResetPass_Timestamp", DateTime.Now.ToString());
            HttpContext.Session.Remove("OTP_ResetPass");
            HttpContext.Session.Remove("OTP_ResetPass_Timestamp");
            HttpContext.Session.Remove("FailedOTP_ResetPass_Attempts");

            return Json(new { success = true, message = "OTP hợp lệ!", redirectUrl = Url.Action("ResetPassword", "Home", new { email }) });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            string otpVerified = HttpContext.Session.GetString("OTP_ResetPass_Verified");
            string otpVerifiedTimestamp = HttpContext.Session.GetString("OTPVerified_ResetPass_Timestamp");
            string verifiedEmail = HttpContext.Session.GetString("ForgotPass_Email");

            if (string.IsNullOrEmpty(email) || otpVerified != "true" || verifiedEmail != email ||string.IsNullOrEmpty(otpVerifiedTimestamp) ||
        (DateTime.TryParse(otpVerifiedTimestamp, out DateTime verifiedTime) && (DateTime.Now - verifiedTime).TotalMinutes > 3))
            {
                HttpContext.Session.Remove("OTP_ResetPass_Verified");
                HttpContext.Session.Remove("OTPVerified_ResetPass_Timestamp");
                HttpContext.Session.Remove("ForgotPass_Email");
                return RedirectToAction("Login");
            }

            return View("ResetPassword", email);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }


            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == model.Email);
            if (employee == null)
            {
                return Json(new { success = false, message = "Email không tồn tại." });
            }

            // Cập nhật mật khẩu mới (cần mã hóa trước khi lưu)
            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("OTP_ResetPass_Verified");
            HttpContext.Session.Remove("ForgotPass_Email");

            return Json(new { success = true, message = "Mật khẩu đã được cập nhật thành công!", redirectUrl = Url.Action("Login", "Home") });
        }

        [HttpPost]
        public async Task<IActionResult> ResendPasswordOTP([FromBody] string email)
        {
            // Kiểm tra xem email có khớp với email đang đăng ký trong Session không
            string sessionEmail = HttpContext.Session.GetString("ForgotPass_Email");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn hoặc email không hợp lệ!" });
            }

            // Tạo OTP mới
            string newOtp = new Random().Next(100000, 999999).ToString();

            // Cập nhật OTP và thời gian vào Session
            HttpContext.Session.SetString("OTP_ResetPass", newOtp);
            HttpContext.Session.SetString("OTP_ResetPass_Timestamp", DateTime.Now.ToString());

            // Đặt lại số lần nhập sai OTP
            HttpContext.Session.SetInt32("FailedOTP_ResetPass_Attempts", 0);

            // Gửi OTP qua email
            var emailService = new EmailService(_configuration);
            emailService.SendOTP(email, newOtp);

            return Json(new { success = true, message = "OTP mới đã được gửi!" });
        }


        [HttpPost]
        public IActionResult CheckEmailExists([FromBody] string email)
        {
            email = email?.Trim().ToLower(); // Chuẩn hóa email để tránh lỗi
            bool exists = _context.Employees.Any(u => u.Email.ToLower() == email);
            return Json(exists);
        }


    }
}
