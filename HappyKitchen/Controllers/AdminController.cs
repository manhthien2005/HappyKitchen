using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyKitchen.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPermissionService _permissionService;
        private readonly EmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, IConfiguration configuration,
            IPermissionService permissionService, EmailService emailService, ILogger<AdminController> logger)
        {
            _context = context;
            _configuration = configuration;
            _permissionService = permissionService;
            _emailService = emailService;
            _logger = logger;
        }

        public IActionResult Login()
        {
            // Check if user is already logged in
            if (HttpContext.Session.GetString("StaffID") != null)
            {
                _logger.LogInformation("User already logged in, redirecting to Dashboard");
                return RedirectToAction("Index", "Dashboard");
            }

            _logger.LogInformation("Login page accessed");
            var email = Request.Cookies["RememberMe_Email"];
            _logger.LogInformation($"RememberMe cookie found: {!string.IsNullOrEmpty(email)}");

            if (!string.IsNullOrEmpty(email))
            {
                _logger.LogInformation($"Attempting auto-login with email: {email}");
                var user = _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Email == email && u.UserType == 1 && u.Status == 0);
                
                if (user != null)
                {
                    _logger.LogInformation($"Auto-login successful for user: {user.UserID}");
                    HttpContext.Session.SetString("StaffID", user.UserID.ToString());
                    HttpContext.Session.SetString("StaffFullName", user.FullName);
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("Phone", user.PhoneNumber);
                    HttpContext.Session.SetString("RoleID", user.RoleID?.ToString() ?? "0");
                    HttpContext.Session.SetString("RoleName", user.Role?.RoleName ?? "");

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    _logger.LogWarning($"Auto-login failed: User not found or inactive for email: {email}");
                    // Clear the invalid RememberMe cookie to prevent login loops
                    Response.Cookies.Delete("RememberMe_Email");
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] EmployeeLogin model)
        {
            _logger.LogInformation($"Login attempt for email: {model?.Email}");
            Console.WriteLine($"Login model: {JsonConvert.SerializeObject(model)}");
            
            if (model == null || string.IsNullOrEmpty(model.Email) ||
                string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.RecaptchaToken))
            {
                _logger.LogWarning("Login failed: Invalid data provided");
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            // Kiểm tra reCAPTCHA
            _logger.LogInformation("Verifying reCAPTCHA");
            bool isRecaptchaValid = await VerifyRecaptcha(model.RecaptchaToken);
            if (!isRecaptchaValid)
            {
                _logger.LogWarning("Login failed: reCAPTCHA verification failed");
                return Json(new { success = false, message = "Xác thực reCAPTCHA thất bại." });
            }

            _logger.LogInformation("Querying user from database");
            var user = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserType == 1 && u.Status == 0);

            
            if (user == null)
            {
                _logger.LogWarning($"Login failed: User not found for email: {model.Email}");
                return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });
            }

            _logger.LogInformation("Verifying password");
            bool passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            if (!passwordValid || user.UserType != 1)
            {
                _logger.LogWarning($"Login failed: Invalid password for user: {user.UserID}");
                return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });
            }

            
            // Kiểm tra cookie trusted device
            _logger.LogInformation("Checking for trusted device");
            if (Request.Cookies.ContainsKey("TrustedDevice"))
            {
                string deviceToken = Request.Cookies["TrustedDevice"];
                _logger.LogInformation($"Found device token: {deviceToken}");
                
                var trustedDevice = _context.TrustedDevices
                    .FirstOrDefault(td => td.DeviceToken == deviceToken && td.UserID == user.UserID);
                
                if (trustedDevice != null)
                {
                    _logger.LogInformation($"Trusted device found for user: {user.UserID}");
                    // Lưu thông tin vào session
                    HttpContext.Session.SetString("StaffID", user.UserID.ToString());
                    HttpContext.Session.SetString("StaffFullName", user.FullName);
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("Phone", user.PhoneNumber);
                    HttpContext.Session.SetString("RoleID", user.RoleID?.ToString() ?? "0");
                    HttpContext.Session.SetString("RoleName", user.Role?.RoleName ?? "");
                    
                    if (model.RememberMe)
                    {
                        _logger.LogInformation("Setting RememberMe cookie");
                        CookieOptions options = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(7),
                            HttpOnly = true,
                            Secure = true
                        };

                        Response.Cookies.Append("RememberMe_Email", user.Email, options);
                    }

                    // Thiết bị đã tin cậy, đăng nhập thành công
                    _logger.LogInformation($"Login successful for user: {user.UserID}");
                    return Json(new { success = true, message = "Đăng nhập thành công!" });
                }
                else
                {
                    _logger.LogWarning("Device token not valid for this user");
                }
            }

            // Thiết bị chưa tin cậy, cần xác thực OTP
            _logger.LogInformation("Device not trusted, generating OTP");
            string otpCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("User_OTP_Login", otpCode);
            HttpContext.Session.SetString("User_Login_Email", model.Email);
            HttpContext.Session.SetString("User_OTP_Login_Timestamp", DateTime.Now.ToString());

            // (Gọi dịch vụ gửi OTP qua email)
            _logger.LogInformation($"Sending OTP to email: {model.Email}");
            var emailService = new EmailService(_configuration);
            try {
                emailService.SendLoginOTP(model.Email, otpCode);
                _logger.LogInformation("OTP sent successfully");
            }
            catch (Exception ex) {
                _logger.LogError($"Error sending OTP: {ex.Message}");
            }

            _logger.LogInformation("Redirecting to OTP verification");
            return Json(new
            {
                success = true,
                requireOTP = true,
                message = "Thiết bị của bạn chưa được tin cậy. OTP đã được gửi đến email. Vui lòng xác thực OTP.",
                redirectUrl = Url.Action("Verify_Login", "Admin", new { email = model.Email })
            });
        }

        // Hàm xác minh reCAPTCHA
        private async Task<bool> VerifyRecaptcha(string recaptchaToken)
        {
            _logger.LogInformation("Verifying reCAPTCHA token");
            string secretKey = "6Le7Le8qAAAAAFfAqwHWHvPrVToCuSXafwzobYgV";
            
            try {
                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaToken}", null);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"reCAPTCHA response: {jsonResponse}");
                    
                    dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                    bool isValid = result.success == true;
                    _logger.LogInformation($"reCAPTCHA verification result: {isValid}");
                    return isValid;
                }
            }
            catch (Exception ex) {
                _logger.LogError($"reCAPTCHA verification error: {ex.Message}");
                return false;
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
        public async Task<IActionResult> Verify_Login([FromBody]  OTPModel model)
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

            // So sánh OTP nhập vào với OTP trong Session
            if (model.OTPCode != sessionOTP)
            {
                return Json(new { success = false, message = "Mã OTP không chính xác." });
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.UserType == 1 && u.Status == 0);
            
            
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
            HttpContext.Session.SetString("StaffID", user.UserID.ToString());
            HttpContext.Session.SetString("StaffFullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Phone", user.PhoneNumber);
            HttpContext.Session.SetString("RoleID", user.RoleID?.ToString() ?? "0");
            HttpContext.Session.SetString("RoleName", user.Role?.RoleName ?? "");

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

            // Trả về kết quả thành công, chuyển hướng (ví dụ: đến trang Menu)
            return Json(new
            {
                success = true,
                message = "Xác thực OTP thành công. Thiết bị của bạn đã được lưu tin cậy.",
                redirectUrl = Url.Action("index", "Dashboard")
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

            var emailService = new EmailService(_configuration);
            emailService.SendLoginOTP(sessionEmail, newOtp);

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
                HttpContext.Session.SetString("StaffFullName", model.FullName);
                HttpContext.Session.SetString("Email", model.Email);
                HttpContext.Session.SetString("Password", model.Password);
                HttpContext.Session.SetString("PhoneNumber", model.PhoneNumber);

                // Tạo và lưu OTP vào Session
                string otpCode = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("User_OTP_SignUp", otpCode);
                HttpContext.Session.SetString("User_OTP_SignUp_Timestamp", DateTime.Now.ToString());

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
                UserType = 1
            };

            _context.Users.Add(newEmployee);
            await _context.SaveChangesAsync();

            return Json(new { success = true, redirectUrl = Url.Action("Login", "Admin") });
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
            HttpContext.Session.SetString("User_OTP_ResetPass", otpCode);
            HttpContext.Session.SetString("User_OTP_ResetPass_Timestamp", DateTime.Now.ToString());

            // Gửi email OTP
            var emailService = new EmailService(_configuration);
            emailService.SendResetPasswordOTP(email, otpCode);

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

            return Json(new { success = true, message = "OTP hợp lệ!", redirectUrl = Url.Action("ResetPassword", "Admin", new { email }) });
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


            var employee = await _context.Users.FirstOrDefaultAsync(e => e.Email == model.Email);
            if (employee == null)
            {
                return Json(new { success = false, message = "Email không tồn tại." });
            }

            // Cập nhật mật khẩu mới (cần mã hóa trước khi lưu)
            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Users.Update(employee);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("User_OTP_ResetPass_Verified");
            HttpContext.Session.Remove("User_ForgotPass_Email");

            return Json(new { success = true, message = "Mật khẩu đã được cập nhật thành công!", redirectUrl = Url.Action("Login", "Admin") });
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
            var emailService = new EmailService(_configuration);
            emailService.SendOTP(email, newOtp);

            return Json(new { success = true, message = "OTP mới đã được gửi!" });
        }


        [HttpPost]
        public IActionResult CheckEmailExists([FromBody] string email)
        {
            email = email?.Trim().ToLower(); // Chuẩn hóa email để tránh lỗi
            bool exists = _context.Users.Any(u => u.Email.ToLower() == email);
            return Json(exists);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RememberMe_Email");
            Response.Cookies.Delete("TrustedDevice");
            return RedirectToAction("Login");
        }

        
        [HttpGet]
        public async Task<IActionResult> GetCurrentUserPermissions()
        {
            var userIdString = HttpContext.Session.GetString("StaffID");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            var permissions = await _permissionService.GetUserPermissionsAsync(userId);
            return Json(new { success = true, data = permissions });
        }
        [HttpGet]
        public async Task<IActionResult> GetUserProfile()
        {
            _logger.LogDebug("[API] GetUserProfile");

            try
            {
                var userIdString = HttpContext.Session.GetString("StaffID");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    _logger.LogWarning("GetUserProfile failed: Invalid or missing StaffID in session");
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                var user = await _context.Users
                    .Where(u => u.UserID == userId && u.UserType == 1 && u.Status == 0)
                    .Select(u => new
                    {
                        u.UserID,
                        u.FullName,
                        u.Email,
                        u.Address
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("GetUserProfile failed: User with UserID {UserID} not found or inactive", userId);
                    return Json(new { success = false, message = "Người dùng không hợp lệ hoặc không hoạt động." });
                }

                return Json(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy thông tin cá nhân" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdateModel model)
        {
            _logger.LogDebug("[API] UpdateUserProfile: UserID={UserID}, FullName={FullName}, Email={Email}", model.UserID, model.FullName, model.Email);

            try
            {
                // Validate model
                if (model == null)
                {
                    _logger.LogWarning("UpdateUserProfile failed: Model is null");
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Validate session
                var userIdString = HttpContext.Session.GetString("StaffID");
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("UpdateUserProfile failed: StaffID is missing in session");
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                if (!int.TryParse(userIdString, out int sessionUserId) || sessionUserId != model.UserID)
                {
                    _logger.LogWarning("UpdateUserProfile failed: Invalid StaffID format or mismatched UserID, StaffID={StaffID}, ModelUserID={ModelUserID}", userIdString, model.UserID);
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                // Find user
                var user = await _context.Users
                    .Where(u => u.UserID == model.UserID && u.UserType == 1 && u.Status == 0)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("UpdateUserProfile failed: User with UserID {UserID} not found or inactive", model.UserID);
                    return Json(new { success = false, message = "Người dùng không hợp lệ hoặc không hoạt động." });
                }

                // Validation
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    _logger.LogWarning("UpdateUserProfile failed: FullName is required");
                    return Json(new { success = false, message = "Họ và tên là bắt buộc" });
                }
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    _logger.LogWarning("UpdateUserProfile failed: Email is required");
                    return Json(new { success = false, message = "Email là bắt buộc" });
                }
                if (model.Password != null && model.Password != model.ConfirmPassword)
                {
                    _logger.LogWarning("UpdateUserProfile failed: Password and ConfirmPassword do not match");
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
                }

                // Check for duplicate email
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserID != model.UserID);
                if (emailExists)
                {
                    _logger.LogWarning("UpdateUserProfile failed: Email {Email} already exists", model.Email);
                    return Json(new { success = false, message = "Email đã được sử dụng" });
                }

                // Update user
                user.FullName = model.FullName.Trim();
                user.Email = model.Email.Trim();
                user.Address = model.Address?.Trim();
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Update session
                HttpContext.Session.SetString("StaffFullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);

                return Json(new { success = true, message = "Cập nhật thông tin thành công" });
            }
            catch (DbUpdateException dbEx)
            {
                return Json(new { success = false, message = "Lỗi cơ sở dữ liệu khi cập nhật thông tin" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật thông tin: " + ex.Message });
            }
        }

        public class UserProfileUpdateModel
        {
            public int UserID { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
        }
        [HttpGet]
        public IActionResult UserProfile()
        {
            _logger.LogDebug("[View] UserProfile");

            try
            {
                var userIdString = HttpContext.Session.GetString("StaffID");
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("UserProfile failed: StaffID is missing in session");
                    return RedirectToAction("Login");
                }

                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("Login");
            }
        }
    }
}