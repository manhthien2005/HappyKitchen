using HappyKitchen.Attributes;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace HappyKitchen.Controllers
{
    [AuthorizeAccess]
    public class CustomerManageController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<CustomerManageController> _logger;

        public CustomerManageController(IUserService userService, ILogger<CustomerManageController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "view")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "view")]
        public IActionResult OrderDetail(int orderId)
        {
            if (orderId <= 0)
            {
                return BadRequest("Order ID không hợp lệ");
            }

            ViewBag.Title = $"Chi tiết đơn hàng #{orderId} - Happy Kitchen";
            ViewBag.OrderId = orderId; 
            return View();
        }
        [HttpGet]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "view")]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 8, 
            string searchTerm = "", string status = "all", string sortBy = "name_asc")
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(0);
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    users = users.Where(u => 
                        u.FullName.ToLower().Contains(searchTerm) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm))
                    ).ToList();
                }
                
                if (status != "all")
                {
                    int statusValue = int.Parse(status);
                    users = users.Where(u => u.Status == statusValue).ToList();
                }
                
                users = sortBy switch
                {
                    "name_asc" => users.OrderBy(u => u.FullName).ToList(),
                    "name_desc" => users.OrderByDescending(u => u.FullName).ToList(),
                    _ => users.OrderBy(u => u.FullName).ToList()
                };

                int totalItems = users.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                
                var pagedUsers = users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                    
                return Json(new { 
                    success = true, 
                    data = pagedUsers.Select(u => new {
                        u.UserID,
                        u.FullName,
                        u.PhoneNumber,
                        u.Email,
                        u.Address,
                        u.UserType,
                        u.Status,
                        totalOrders = u.Orders.Count,
                        totalSpent = u.Orders.Sum(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity)),
                        lastOrderDate = u.Orders.OrderByDescending(o => o.OrderTime).FirstOrDefault()?.OrderTime
                    }),
                    pagination = new {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách người dùng");
                return Json(new { success = false, message = "Lỗi khi lấy danh sách người dùng" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "add")]
        public async Task<JsonResult> CreateUser([FromBody] User user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.FullName) || user.FullName.Length > 100)
                    return Json(new { success = false, message = "Họ tên bắt buộc và phải <= 100 ký tự" });
                
                if (string.IsNullOrWhiteSpace(user.Email))
                    return Json(new { success = false, message = "Email bắt buộc" });
                
                if (string.IsNullOrWhiteSpace(user.PhoneNumber) || 
                    !new Regex(@"^[0-9]{10,15}$").IsMatch(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại phải có 10-15 chữ số" });
                
                if (!new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$").IsMatch(user.Email))
                    return Json(new { success = false, message = "Định dạng email không hợp lệ" });
                
                if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash.Length < 6)
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
                
                if (!string.IsNullOrEmpty(user.Address) && user.Address.Length > 200)
                    return Json(new { success = false, message = "Địa chỉ phải <= 200 ký tự" });

                if (await _userService.GetUserByEmailAsync(user.Email) != null)
                    return Json(new { success = false, message = "Email đã được sử dụng" });

                if (await _userService.GetUserByPhoneAsync(user.PhoneNumber) != null)
                    return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });

                user.PasswordHash = PasswordService.HashPassword(user.PasswordHash);
                await _userService.CreateUserAsync(user);
                _logger.LogInformation("Người dùng được tạo thành công");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo người dùng");
                return Json(new { success = false, message = "Lỗi khi tạo người dùng" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "edit")]
        public async Task<JsonResult> UpdateUser([FromBody] UserUpdateModel model)
        {
            try
            {
                if (model?.User == null)
                    return Json(new { success = false, message = "Dữ liệu người dùng bắt buộc" });

                var user = model.User;

                if (string.IsNullOrWhiteSpace(user.FullName) || user.FullName.Length > 100)
                    return Json(new { success = false, message = "Họ tên bắt buộc và phải <= 100 ký tự" });
                
                if (string.IsNullOrWhiteSpace(user.PhoneNumber) || 
                    !new Regex(@"^[0-9]{10,15}$").IsMatch(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại phải có 10-15 chữ số" });
                
                if (string.IsNullOrWhiteSpace(user.Email) || 
                    !new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$").IsMatch(user.Email))
                    return Json(new { success = false, message = "Định dạng email không hợp lệ" });
                
                if (!string.IsNullOrEmpty(user.Address) && user.Address.Length > 200)
                    return Json(new { success = false, message = "Địa chỉ phải <= 200 ký tự" });

                var existingUser = await _userService.GetUserByIdAsync(user.UserID);
                if (existingUser == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });

                if (await _userService.GetUserByEmailAsync(user.Email) is User emailUser && 
                    emailUser.UserID != user.UserID)
                    return Json(new { success = false, message = "Email đã được sử dụng" });

                if (await _userService.GetUserByPhoneAsync(user.PhoneNumber) is User phoneUser && 
                    phoneUser.UserID != user.UserID)
                    return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });

                if (model.UpdatePassword)
                {
                    if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 6)
                        return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
                    user.PasswordHash = PasswordService.HashPassword(model.NewPassword);
                }
                else
                {
                    user.PasswordHash = existingUser.PasswordHash;
                }

                await _userService.UpdateUserAsync(user);
                _logger.LogInformation("Người dùng được cập nhật thành công: {UserId}", user.UserID);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật người dùng");
                return Json(new { success = false, message = "Lỗi khi cập nhật người dùng" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "delete")]
        public async Task<JsonResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                _logger.LogInformation("Người dùng được xóa thành công: {UserId}", id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa người dùng");
                return Json(new { success = false, message = "Lỗi khi xóa người dùng" });
            }
        }

        [HttpGet]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "view")]
        public async Task<IActionResult> GetUserOrders(int userId, int page = 1, int pageSize = 5)
        {
            try
            {
                var orders = await _userService.GetOrdersByUserAsync(userId);
                if (orders == null || !orders.Any())
                {
                    return Json(new 
                    { 
                        success = true, 
                        data = new object[] {},
                        pagination = new 
                        {
                            currentPage = page,
                            pageSize = pageSize,
                            totalItems = 0,
                            totalPages = 0
                        }
                    });
                }

                int totalItems = orders.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var pagedOrders = orders
                    .OrderByDescending(o => o.OrderTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new 
                { 
                    success = true, 
                    data = pagedOrders.Select(o => new 
                    {
                        o.OrderID,
                        OrderTime = o.OrderTime.ToString("o"),
                        o.Status,
                        Total = o.OrderDetails?.Sum(od => od.MenuItem.Price * od.Quantity) ?? 0,
                        Items = o.OrderDetails?.Select(od => new 
                        {
                            Name = od.MenuItem.Name,
                            Price = od.MenuItem.Price,
                            od.Quantity
                        }) ?? Enumerable.Empty<object>()
                    }),
                    pagination = new 
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng của người dùng {UserId}", userId);
                return Json(new { success = false, message = "Lỗi khi lấy đơn hàng" });
            }
        }

        [HttpGet]
        [AuthorizeAccess("CUSTOMER_ACCOUNT_MANAGE", "view")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _userService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        order = new
                        {
                            order.OrderID,
                            OrderTime = order.OrderTime.ToString("o"),
                            order.Status,
                            Total = order.OrderDetails?.Sum(od => od.MenuItem.Price * od.Quantity) ?? 0,
                            Items = order.OrderDetails?.Select(od => new
                            {
                                Name = od.MenuItem.Name,
                                UnitPrice = od.MenuItem.Price,
                                od.Quantity,
                                Price = od.MenuItem.Price * od.Quantity
                            }) ?? Enumerable.Empty<object>()
                        },
                        customer = order.Customer != null ? new
                        {
                            UserID = order.Customer.UserID,
                            FullName = order.Customer.FullName ?? "Không có tên",
                            PhoneNumber = order.Customer.PhoneNumber ?? "Chưa cập nhật",
                            Email = order.Customer.Email ?? "Chưa cập nhật",
                            Address = order.Customer.Address ?? "Chưa cập nhật"
                        } : new
                        {
                            UserID = 0,
                            FullName = "Khách vãng lai",
                            PhoneNumber = "Chưa cập nhật",
                            Email = "Chưa cập nhật",
                            Address = "Chưa cập nhật"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đơn hàng {OrderId}", orderId);
                return Json(new { success = false, message = "Lỗi khi lấy chi tiết đơn hàng" });
            }
        }
    }
}