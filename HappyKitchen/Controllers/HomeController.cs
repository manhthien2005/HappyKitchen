using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        public IActionResult Login([FromBody] EmployeeLogin model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var user = _context.Employees
                .AsNoTracking()
                .FirstOrDefault(e => e.Email == model.Email);
            if (user == null)
            {
                return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });
            }

            return Json(new { success = true, message = "Đăng nhập thành công!" });
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
                HttpContext.Session.SetString("OTP", otpCode);

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
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("SignUp");
            }

            return View("VerifyOTP", email);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTPCheck([FromBody] OTPModel model)
        {   
            string sessionOTP = HttpContext.Session.GetString("OTP");
            string fullName = HttpContext.Session.GetString("FullName");
            string email = HttpContext.Session.GetString("Email");
            string password = HttpContext.Session.GetString("Password");
            string phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            string otpTimestamp = HttpContext.Session.GetString("OTPTimestamp");
            int failedAttempts = HttpContext.Session.GetInt32("FailedOTPAttempts") ?? 0;

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn, vui lòng thử lại." });
            }

            // Kiểm tra thời gian hết hạn OTP (5 phút)
            if (DateTime.TryParse(otpTimestamp, out DateTime otpTime) && (DateTime.Now - otpTime).TotalMinutes > 5)
            {
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("OTPTimestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng thử lại." });
            }

            // Kiểm tra số lần nhập sai OTP
            if (failedAttempts >= 3)
            {
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("OTPTimestamp");
                return Json(new { success = false, message = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã mới." });
            }

            if (model.OTPCode != sessionOTP)
            {
                HttpContext.Session.SetInt32("FailedOTPAttempts", failedAttempts + 1);
                return Json(new { success = false, message = "Mã OTP không chính xác. Vui lòng thử lại." });
            }

            // Xóa OTP khỏi session sau khi xác thực thành công
            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("OTPTimestamp");
            HttpContext.Session.Remove("FailedOTPAttempts");

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
            HttpContext.Session.SetString("OTP", newOtp);
            HttpContext.Session.SetString("OTPTimestamp", DateTime.Now.ToString());

            // Đặt lại số lần nhập sai OTP
            HttpContext.Session.SetInt32("FailedOTPAttempts", 0);

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
            HttpContext.Session.SetString("OTPPassword", otpCode);
            HttpContext.Session.SetString("OTPPassTimestamp", DateTime.Now.ToString());

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
            string sessionOTP = HttpContext.Session.GetString("OTPPassword");
            string email = HttpContext.Session.GetString("ForgotPass_Email");
            string otpTimestamp = HttpContext.Session.GetString("OTPTimestamp");
            int failedAttempts = HttpContext.Session.GetInt32("FailedOTPAttempts") ?? 0;

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn, vui lòng thử lại." });
            }

            // Kiểm tra thời gian hết hạn OTP (5 phút)
            if (DateTime.TryParse(otpTimestamp, out DateTime otpTime) && (DateTime.Now - otpTime).TotalMinutes > 5)
            {
                HttpContext.Session.Remove("OTPPassword");
                HttpContext.Session.Remove("OTPPassTimestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng thử lại." });
            }

            // Kiểm tra số lần nhập sai OTP
            if (failedAttempts >= 3)
            {
                HttpContext.Session.Remove("OTPPassword");
                HttpContext.Session.Remove("OTPPassTimestamp");
                return Json(new { success = false, message = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã mới." });
            }

            if (model.OTPPassCode != sessionOTP)
            {
                HttpContext.Session.SetInt32("FailedOTPAttempts", failedAttempts + 1);
                return Json(new { success = false, message = "Mã OTP không chính xác. Vui lòng thử lại." });
            }

            // Nếu OTP hợp lệ, xóa OTP khỏi session và chuyển đến trang đặt lại mật khẩu
            HttpContext.Session.Remove("OTPPassword");
            HttpContext.Session.Remove("OTPPassTimestamp");
            HttpContext.Session.Remove("FailedOTPAttempts");

            return Json(new { success = true, message = "OTP hợp lệ!", redirectUrl = Url.Action("ResetPassword", "Home", new { email }) });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
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
            HttpContext.Session.SetString("OTPPassword", newOtp);
            HttpContext.Session.SetString("OTPTimestamp", DateTime.Now.ToString());

            // Đặt lại số lần nhập sai OTP
            HttpContext.Session.SetInt32("FailedOTPAttempts", 0);

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
