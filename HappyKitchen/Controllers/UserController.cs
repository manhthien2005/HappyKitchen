﻿using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static QRCoder.PayloadGenerator;

namespace HappyKitchen.Controllers
{
    public class UserController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IUserService _userService;

        public UserController(ApplicationDbContext context, IConfiguration configuration, EmailService emailService, IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _userService = userService;
        }

        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Tạo ViewModel để truyền dữ liệu
            var viewModel = new UserProfileViewModel
            {
                User = user,
                IsEmailVerified = !string.IsNullOrEmpty(user.Email),
                IsPhoneVerified = !string.IsNullOrEmpty(user.PhoneNumber)
            };

            return View(viewModel);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] EmployeeLogin model)
        {
            Console.WriteLine(model);
            if (model == null || string.IsNullOrEmpty(model.Email) ||
                string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.RecaptchaToken))
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
            var user = _context.Users.AsNoTracking().FirstOrDefault(e => e.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash) || (user.UserType != 0 && user.UserType != 1))
            {
                return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });
            }

            // Kiểm tra trạng thái người dùng
            if (user.Status == 1 || user.Status == 2)
            {
                return Json(new { success = false, message = "Tài khoản của bạn đã bị đình chỉ." });
            }

            // Kiểm tra cookie trusted device
            if (Request.Cookies.ContainsKey("TrustedDevice"))
            {
                string deviceToken = Request.Cookies["TrustedDevice"];
                var trustedDevice = _context.TrustedDevices
                    .FirstOrDefault(td => td.DeviceToken == deviceToken && td.UserID == user.UserID);
                if (trustedDevice != null)
                {
                    // Lưu thông tin vào session
                    HttpContext.Session.SetString("FullName", user.FullName);
                    HttpContext.Session.SetInt32("UserID", user.UserID);
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("Phone", user.PhoneNumber);
                    //khoa
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim("Phone", user.PhoneNumber)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe, // Lưu cookie lâu dài nếu chọn "Remember Me"
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };
                    await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

                    // Kiểm tra "Remember Me"
                    if (model.RememberMe)
                    {
                        CookieOptions options = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(7),
                            HttpOnly = true,
                            Secure = true
                        };

                        Response.Cookies.Append("RememberMe_Email", user.Email, options);
                    }
                    // Thiết bị đã tin cậy, đăng nhập thành công
                    return Json(new { success = true, message = "Đăng nhập thành công!" });
                }
            }

            string otpCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("User_OTP_Login", otpCode);
            HttpContext.Session.SetString("User_Login_Email", model.Email);
            HttpContext.Session.SetString("User_OTP_Login_Timestamp", DateTime.Now.ToString());

            // (Gọi dịch vụ gửi OTP qua email)
            _emailService.SendLoginOTP(model.Email, otpCode);

            return Json(new
            {
                success = true,
                requireOTP = true,
                message = "Thiết bị của bạn chưa được tin cậy. OTP đã được gửi đến email. Vui lòng xác thực OTP.",
                redirectUrl = Url.Action("Verify_Login", "User", new { email = model.Email })
            });
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

        [HttpGet]
        public IActionResult Verify_Login(string email)
        {
            string sessionEmail = HttpContext.Session.GetString("User_Login_Email");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                return RedirectToAction("SignUp");
            }

            return View("Verify_Login", email);
        }

        [HttpPost]
        public IActionResult Verify_Login([FromBody] OTPModel model)
        {
            // Kiểm tra dữ liệu đầu vào
            if (model == null || string.IsNullOrEmpty(model.OTPCode))
            {
                return Json(new { success = false, message = "Mã OTP không hợp lệ." });
            }

            // Lấy OTP và email từ Session
            string sessionOTP = HttpContext.Session.GetString("User_OTP_Login");
            string email = HttpContext.Session.GetString("User_Login_Email");

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên OTP đã hết hạn. Vui lòng đăng nhập lại." });
            }

            // Kiểm tra thời gian OTP (ví dụ: 5 phút)
            if (DateTime.TryParse(HttpContext.Session.GetString("User_OTP_Login_Timestamp"), out DateTime ts) &&
                (DateTime.Now - ts).TotalMinutes > 5)
            {
                HttpContext.Session.Remove("User_OTP_Login");
                HttpContext.Session.Remove("User_Login_Email");
                HttpContext.Session.Remove("User_OTP_Login_Timestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng đăng nhập lại." });
            }

            var user = _context.Users.FirstOrDefault(e => e.Email == email);
            // So sánh OTP nhập vào với OTP trong Session
            if (model.OTPCode != sessionOTP)
            {
                //khoa
                // Tạo claims cho người dùng
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim("Phone", user.PhoneNumber)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                // Đăng nhập người dùng
                HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties).GetAwaiter().GetResult();
                return Json(new { success = false, message = "Mã OTP không chính xác." });
            }

            // OTP hợp lệ, tiến hành xác định thiết bị tin cậy
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            // Tạo token duy nhất cho thiết bị (GUID)
            string trustedDeviceToken = Guid.NewGuid().ToString();

            // Lưu thông tin thiết bị vào DB
            TrustedDevice device = new TrustedDevice
            {
                UserID = user.UserID,
                DeviceToken = trustedDeviceToken,
                CreatedAt = DateTime.Now
            };

            _context.TrustedDevices.Add(device);
            _context.SaveChanges();

            // Xóa dữ liệu OTP khỏi session
            HttpContext.Session.Remove("User_OTP_Login");
            HttpContext.Session.Remove("User_Login_Email");
            HttpContext.Session.Remove("User_OTP_Login_Timestamp");

            // Đánh dấu OTP đã xác thực
            HttpContext.Session.SetString("User_OTP_Login_Verified", "true");

            // Gửi cookie TrustedDevice (thời hạn 30 ngày)
            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                Secure = true
            };
            Response.Cookies.Append("TrustedDevice", trustedDeviceToken, options);

            // Lưu thông tin vào session
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Phone", user.PhoneNumber);

            // Kiểm tra "Remember Me"
            if (model.RememberMe)
            {
                options = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(7),
                    HttpOnly = true,
                    Secure = true
                };

                Response.Cookies.Append("RememberMe_Email", user.Email, options);
            }

            return Json(new
            {
                success = true,
                message = "Xác thực OTP thành công. Thiết bị của bạn đã được lưu tin cậy.",
                redirectUrl = Url.Action("Menu", "Home")
            });
        }

        [HttpPost]
        public async Task<IActionResult> Resend_Login_OTP([FromBody] string email)
        {
            // Kiểm tra email có khớp với email đăng nhập trong session không
            string sessionEmail = HttpContext.Session.GetString("User_Login_Email");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn hoặc email không hợp lệ!" });
            }

            // Tạo OTP mới và lưu vào session
            string newOtp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("User_OTP_Login", newOtp);
            HttpContext.Session.SetString("User_OTP_Login_Timestamp", DateTime.Now.ToString());

            _emailService.SendLoginOTP(sessionEmail, newOtp);

            return Json(new { success = true, message = "OTP mới đã được gửi!" });
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(EmployeeRegister model)
        {
            if (ModelState.IsValid)
            {
                bool emailExists = await _context.Users.AnyAsync(e => e.Email == model.Email);
                bool phoneExists = await _context.Users.AnyAsync(e => e.PhoneNumber == model.PhoneNumber);

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
                HttpContext.Session.SetString("User_FullName", model.FullName);
                HttpContext.Session.SetString("User_Email", model.Email);
                HttpContext.Session.SetString("User_Password", model.Password);
                HttpContext.Session.SetString("User_PhoneNumber", model.PhoneNumber);

                // Tạo và lưu OTP vào Session
                string otpCode = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("User_OTP_SignUp", otpCode);
                HttpContext.Session.SetString("User_OTP_SignUp_Timestamp", DateTime.Now.ToString());

                // Gửi email OTP
                _emailService.SendOTP(model.Email, otpCode);

                return RedirectToAction("VerifyOTP", new { email = model.Email });
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult VerifyOTP(string email)
        {
            string sessionEmail = HttpContext.Session.GetString("User_Email");
            string sessionOTP = HttpContext.Session.GetString("User_OTP_SignUp");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sessionEmail) || string.IsNullOrEmpty(sessionOTP) || sessionEmail != email)
            {
                return RedirectToAction("SignUp");
            }

            return View("VerifyOTP", email);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTPCheck([FromBody] OTPModel model)
        {
            string sessionOTP = HttpContext.Session.GetString("User_OTP_SignUp");
            string fullName = HttpContext.Session.GetString("User_FullName");
            string email = HttpContext.Session.GetString("User_Email");
            string password = HttpContext.Session.GetString("User_Password");
            string phoneNumber = HttpContext.Session.GetString("User_PhoneNumber");
            string otpTimestamp = HttpContext.Session.GetString("User_OTP_SignUp_Timestamp");
            int failedAttempts = HttpContext.Session.GetInt32("User_FailedOTP_SignUp_Attempts") ?? 0;

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn, vui lòng thử lại." });
            }

            // Kiểm tra thời gian hết hạn OTP (5 phút)
            if (DateTime.TryParse(otpTimestamp, out DateTime otpTime) && (DateTime.Now - otpTime).TotalMinutes > 3)
            {
                HttpContext.Session.Remove("User_OTP_SignUp");
                HttpContext.Session.Remove("User_OTP_SignUp_Timestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng thử lại." });
            }

            // Kiểm tra số lần nhập sai OTP
            if (failedAttempts >= 3)
            {
                HttpContext.Session.Remove("User_OTP_SignUp");
                HttpContext.Session.Remove("User_OTP_SignUp_Timestamp");
                return Json(new { success = false, message = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã mới." });
            }

            if (model.OTPCode != sessionOTP)
            {
                HttpContext.Session.SetInt32("User_FailedOTP_SignUp_Attempts", failedAttempts + 1);
                return Json(new { success = false, message = "Mã OTP không chính xác. Vui lòng thử lại." });
            }

            // Xóa OTP khỏi session sau khi xác thực thành công
            HttpContext.Session.Remove("User_OTP_SignUp");
            HttpContext.Session.Remove("User_OTP_SignUp_Timestamp");
            HttpContext.Session.Remove("User_FailedOTP_SignUp_Attempts");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Lưu nhân viên vào database
            var newEmployee = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = hashedPassword,
                UserType = 0
            };

            _context.Users.Add(newEmployee);
            await _context.SaveChangesAsync();

            return Json(new { success = true, redirectUrl = Url.Action("Login", "User") });
        }

        [HttpPost]
        public async Task<IActionResult> ResendOTP([FromBody] string email)
        {
            // Kiểm tra xem email có khớp với email đang đăng ký trong Session không
            string sessionEmail = HttpContext.Session.GetString("User_Email");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn hoặc email không hợp lệ!" });
            }

            // Tạo OTP mới
            string newOtp = new Random().Next(100000, 999999).ToString();

            // Cập nhật OTP và thời gian vào Session
            HttpContext.Session.SetString("User_OTP_SignUp", newOtp);
            HttpContext.Session.SetString("User_OTP_SignUp_Timestamp", DateTime.Now.ToString());

            // Đặt lại số lần nhập sai OTP
            HttpContext.Session.SetInt32("User_FailedOTP_SignUp_Attempts", 0);

            // Gửi OTP qua email
            _emailService.SendOTP(email, newOtp);

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
            HttpContext.Session.SetString("User_OTP_ResetPass", otpCode);
            HttpContext.Session.SetString("User_OTP_ResetPass_Timestamp", DateTime.Now.ToString());

            // Gửi email OTP
            _emailService.SendResetPasswordOTP(email, otpCode);

            HttpContext.Session.SetString("User_ForgotPass_Email", email);

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
            string sessionOTP = HttpContext.Session.GetString("User_OTP_ResetPass");
            string email = HttpContext.Session.GetString("User_ForgotPass_Email");
            string otpTimestamp = HttpContext.Session.GetString("User_OTP_ResetPass_Timestamp");
            int failedAttempts = HttpContext.Session.GetInt32("User_FailedOTP_ResetPass_Attempts") ?? 0;

            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn, vui lòng thử lại." });
            }

            // Kiểm tra thời gian hết hạn OTP (5 phút)
            if (DateTime.TryParse(otpTimestamp, out DateTime otpTime) && (DateTime.Now - otpTime).TotalMinutes > 5)
            {
                HttpContext.Session.Remove("User_OTP_ResetPass");
                HttpContext.Session.Remove("User_OTP_ResetPass_Timestamp");
                return Json(new { success = false, message = "Mã OTP đã hết hạn. Vui lòng thử lại." });
            }

            // Kiểm tra số lần nhập sai OTP
            if (failedAttempts >= 3)
            {
                HttpContext.Session.Remove("User_OTP_ResetPass");
                HttpContext.Session.Remove("User_OTP_ResetPass_Timestamp");
                return Json(new { success = false, message = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu mã mới." });
            }

            if (model.OTPPassCode != sessionOTP)
            {
                HttpContext.Session.SetInt32("User_FailedOTP_ResetPass_Attempts", failedAttempts + 1);
                return Json(new { success = false, message = "Mã OTP không chính xác. Vui lòng thử lại." });
            }

            // Nếu OTP hợp lệ, xóa OTP khỏi session và chuyển đến trang đặt lại mật khẩu
            HttpContext.Session.SetString("User_OTP_ResetPass_Verified", "true");
            HttpContext.Session.SetString("User_OTPVerified_ResetPass_Timestamp", DateTime.Now.ToString());
            HttpContext.Session.Remove("User_OTP_ResetPass");
            HttpContext.Session.Remove("User_OTP_ResetPass_Timestamp");
            HttpContext.Session.Remove("User_FailedOTP_ResetPass_Attempts");

            return Json(new { success = true, message = "OTP hợp lệ!", redirectUrl = Url.Action("ResetPassword", "User", new { email }) });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            string otpVerified = HttpContext.Session.GetString("User_OTP_ResetPass_Verified");
            string otpVerifiedTimestamp = HttpContext.Session.GetString("User_OTPVerified_ResetPass_Timestamp");
            string verifiedEmail = HttpContext.Session.GetString("User_ForgotPass_Email");

            if (string.IsNullOrEmpty(email) || otpVerified != "true" || verifiedEmail != email || string.IsNullOrEmpty(otpVerifiedTimestamp) ||
        (DateTime.TryParse(otpVerifiedTimestamp, out DateTime verifiedTime) && (DateTime.Now - verifiedTime).TotalMinutes > 3))
            {
                HttpContext.Session.Remove("User_OTP_ResetPass_Verified");
                HttpContext.Session.Remove("User_OTPVerified_ResetPass_Timestamp");
                HttpContext.Session.Remove("User_ForgotPass_Email");
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


            var customer = await _context.Users.FirstOrDefaultAsync(e => e.Email == model.Email);
            if (customer == null)
            {
                return Json(new { success = false, message = "Email không tồn tại." });
            }

            // Cập nhật mật khẩu mới (cần mã hóa trước khi lưu)
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Users.Update(customer);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("User_OTP_ResetPass_Verified");
            HttpContext.Session.Remove("User_ForgotPass_Email");

            return Json(new { success = true, message = "Mật khẩu đã được cập nhật thành công!", redirectUrl = Url.Action("Login", "User") });
        }

        [HttpPost]
        public async Task<IActionResult> ResendPasswordOTP([FromBody] string email)
        {
            // Kiểm tra xem email có khớp với email đang đăng ký trong Session không
            string sessionEmail = HttpContext.Session.GetString("User_ForgotPass_Email");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                return Json(new { success = false, message = "Phiên đăng ký hết hạn hoặc email không hợp lệ!" });
            }
            
            // Tạo OTP mới
            string newOtp = new Random().Next(100000, 999999).ToString();

            // Cập nhật OTP và thời gian vào Session
            HttpContext.Session.SetString("User_OTP_ResetPass", newOtp);
            HttpContext.Session.SetString("User_OTP_ResetPass_Timestamp", DateTime.Now.ToString());

            // Đặt lại số lần nhập sai OTP
            HttpContext.Session.SetInt32("User_FailedOTP_ResetPass_Attempts", 0);

            // Gửi OTP qua email
            _emailService.SendResetPasswordOTP(email, newOtp);

            return Json(new { success = true, message = "OTP mới đã được gửi!" });
        }


        [HttpPost]
        public IActionResult CheckEmailExists([FromBody] string email)
        {
            email = email?.Trim().ToLower(); // Chuẩn hóa email để tránh lỗi
            bool exists = _context.Users.Any(u => u.Email.ToLower() == email);
            return Json(exists);
        }

        [HttpPost]
        public async Task<IActionResult> SendProfileOTP([FromBody] string email)
        {
            try
            {
                // Generate OTP
                string otp = new Random().Next(100000, 999999).ToString();
                
                // Store OTP in session
                HttpContext.Session.SetString("Profile_OTP", otp);
                HttpContext.Session.SetString("Profile_OTP_Timestamp", DateTime.Now.ToString());
                HttpContext.Session.SetString("Profile_OTP_Email", email);

                // Send OTP via email
                _emailService.SendOTP(email, otp);

                return Json(new { success = true, message = "OTP đã được gửi đến email của bạn" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi gửi OTP: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyProfileOTP([FromBody] string otp)
        {
            try
            {
                var storedOtp = HttpContext.Session.GetString("Profile_OTP");
                var otpTimestamp = HttpContext.Session.GetString("Profile_OTP_Timestamp");
                var email = HttpContext.Session.GetString("Profile_OTP_Email");

                if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(otpTimestamp) || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Phiên xác thực đã hết hạn" });
                }

                // Check if OTP is expired (5 minutes)
                if (DateTime.Now - DateTime.Parse(otpTimestamp) > TimeSpan.FromMinutes(5))
                {
                    return Json(new { success = false, message = "Mã OTP đã hết hạn" });
                }

                if (otp != storedOtp)
                {
                    return Json(new { success = false, message = "Mã OTP không chính xác" });
                }

                // Clear OTP session data
                HttpContext.Session.Remove("Profile_OTP");
                HttpContext.Session.Remove("Profile_OTP_Timestamp");
                HttpContext.Session.Remove("Profile_OTP_Email");

                return Json(new { success = true, message = "Xác thực OTP thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xác thực OTP: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateModel model)
        {
            try
            {
                if (model?.User == null)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });
                }

                var user = model.User;
                user.UserID = userId.Value;

                // Validate data
                if (string.IsNullOrWhiteSpace(user.FullName) || user.FullName.Length > 100)
                    return Json(new { success = false, message = "Họ tên không hợp lệ" });

                if (string.IsNullOrWhiteSpace(user.PhoneNumber) ||
                    !new Regex(@"^[0-9]{10,15}$").IsMatch(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ" });

                if (string.IsNullOrWhiteSpace(user.Email) ||
                    !new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$").IsMatch(user.Email))
                    return Json(new { success = false, message = "Email không hợp lệ" });

                // Check if email/phone is already used by another user
                var existingUserWithEmail = await _userService.GetUserByEmailAsync(user.Email);
                if (existingUserWithEmail != null && existingUserWithEmail.UserID != user.UserID)
                    return Json(new { success = false, message = "Email đã được sử dụng" });

                var existingUserWithPhone = await _userService.GetUserByPhoneAsync(user.PhoneNumber);
                if (existingUserWithPhone != null && existingUserWithPhone.UserID != user.UserID)
                    return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });

                // Update user
                await _userService.UpdateUserAsync(user);

                // Update session data
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Phone", user.PhoneNumber);

                return Json(new { success = true, message = "Cập nhật thông tin thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật thông tin: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailVerificationOTP([FromBody] string email)
        {
            try
            {
                // Validate email
                if (string.IsNullOrEmpty(email) || !new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$").IsMatch(email))
                {
                    return Json(new { success = false, message = "Email không hợp lệ" });
                }

                // Check if email exists
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email đã được sử dụng bởi tài khoản khác" });
                }

                // Generate OTP
                string otp = new Random().Next(100000, 999999).ToString();
                Console.WriteLine($"Generated OTP for {email}: {otp}"); // Debug log

                // Store OTP in session
                HttpContext.Session.SetString("Email_Verification_OTP", otp);
                HttpContext.Session.SetString("Email_Verification_Timestamp", DateTime.Now.ToString());
                HttpContext.Session.SetString("Email_Verification_Email", email);

                // Send OTP via email
                try
                {
                    await _emailService.SendEmailVerificationOTP(email, otp);
                    Console.WriteLine($"" +
                        $"OTP sent to {email}"); // Debug log
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}"); // Debug log
                    return Json(new { success = false, message = "Không thể gửi email. Vui lòng thử lại sau." });
                }

                return Json(new { success = true, message = "OTP đã được gửi đến email của bạn" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendEmailVerificationOTP: {ex.Message}"); // Debug log
                return Json(new { success = false, message = "Lỗi khi gửi OTP: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailOTP([FromBody] string otp)
        {
            try
            {
                var storedOtp = HttpContext.Session.GetString("Email_Verification_OTP");
                var otpTimestamp = HttpContext.Session.GetString("Email_Verification_Timestamp");
                var email = HttpContext.Session.GetString("Email_Verification_Email");

                if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(otpTimestamp) || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Phiên xác thực đã hết hạn" });
                }

                // Check if OTP is expired (5 minutes)
                if (DateTime.Now - DateTime.Parse(otpTimestamp) > TimeSpan.FromMinutes(5))
                {
                    return Json(new { success = false, message = "Mã OTP đã hết hạn" });
                }

                if (otp != storedOtp)
                {
                    return Json(new { success = false, message = "Mã OTP không chính xác" });
                }

                // Clear OTP session data
                HttpContext.Session.Remove("Email_Verification_OTP");
                HttpContext.Session.Remove("Email_Verification_Timestamp");
                HttpContext.Session.Remove("Email_Verification_Email");

                return Json(new { success = true, message = "Xác thực email thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xác thực OTP: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn" });
                }

                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Kiểm tra mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
                }

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 8)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 8 ký tự" });
                }

                // Kiểm tra mật khẩu mới có đủ yêu cầu không
                var passwordRegex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[\\@\\$\\!\\%\\*\\?\\&])[A-Za-z\\d\\@\\$\\!\\%\\*\\?\\&]{8,}$");
                if (!passwordRegex.IsMatch(model.NewPassword))
                {
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt" });
                }

                // Cập nhật mật khẩu mới (hash trước khi lưu)
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _userService.UpdateUserAsync(user);

                return Json(new { success = true, message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi đổi mật khẩu: " + ex.Message });
            }
        }

        /*Xem lịch sử của người dùng*/
        public IActionResult ViewOrderHistory()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            Console.WriteLine($"OTP sent to {userId}"); // Debug log

            var orders = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem) // Bao gồm MenuItem để lấy Name, Price
            .Where(o => o.CustomerID == userId)
            .ToList();
            return View(orders);


        }

        public IActionResult ViewReservationHistory()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var reservations = _context.Reservations
                .Include(r => r.Table) // Bao gồm thông tin bàn
                .Include(r => r.Orders) // Bao gồm thông tin đơn hàng liên quan
                .Where(r => r.CustomerID == userId)
                .ToList();

            return View(reservations);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Xóa tất cả session
            HttpContext.Session.Clear();

            // Xóa cookie RememberMe nếu có
            if (Request.Cookies.ContainsKey("RememberMe_Email"))
            {
                Response.Cookies.Delete("RememberMe_Email");
            }

            // Xóa cookie TrustedDevice nếu có
            if (Request.Cookies.ContainsKey("TrustedDevice"))
            {
                Response.Cookies.Delete("TrustedDevice");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
