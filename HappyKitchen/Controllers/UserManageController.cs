using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using HappyKitchen.Services;
using HappyKitchen.Models;
using HappyKitchen.Attributes;

namespace HappyKitchen.Controllers
{
    [AuthorizeAccess]
    public class UserManageController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserManageController> _logger;
        private readonly IPermissionService _permissionService;

        public UserManageController(IUserService userService, IPermissionService permissionService, ILogger<UserManageController> logger)
        {
            _userService = userService;
            _permissionService = permissionService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeAccess("STAFF_ACCOUNT_MANAGE", "view")]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 8, string searchTerm = "", string status = "all")
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(1);
                
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
                        u.RoleID,
                        Role = u.Role != null ? new { 
                            u.Role.RoleID, 
                            u.Role.RoleName 
                        } : null,
                        u.Salary
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
                return Json(new { 
                    success = false, 
                    message = "Error retrieving users: " + ex.Message 
                });
            }
        }

        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "view")]
        public async Task<JsonResult> GetRoles()
        {
            try
            {
                var roles = await _userService.GetAllRolesAsync();
                return Json(new { 
                    success = true, 
                    data = roles.Select(r => new {
                        r.RoleID,
                        r.RoleKey,
                        r.RoleName,
                        r.Description,
                        UserCount = r.Users?.Count ?? 0,
                        Permissions = r.RolePermissions?.Select(rp => new {
                            rp.PermissionID,
                            rp.Permission.PermissionKey,
                            rp.Permission?.PermissionName,
                            rp.CanView,
                            rp.CanAdd,
                            rp.CanEdit,
                            rp.CanDelete
                        })
                    }) 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message 
                });
            }
        }

        [HttpGet]
        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "view")]
        public async Task<JsonResult> GetPermissions()
        {
            try
            {
                var permissions = await _userService.GetAllPermissionsAsync();
                return Json(new { 
                    success = true, 
                    data = permissions.Select(p => new {
                        p.PermissionID,
                        p.PermissionKey,
                        p.PermissionName,
                        p.Description
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "view")]
        public async Task<JsonResult> GetRolePermissions(int roleId)
        {
            try
            {
                var role = await _userService.GetRoleByIdAsync(roleId);
                if (role == null)
                    return Json(new { success = false, message = "Không tìm thấy role" });

                return Json(new { 
                    success = true, 
                    data = role.RolePermissions?.Select(rp => new {
                        rp.RoleID,
                        rp.PermissionID,
                        rp.CanView,
                        rp.CanAdd,
                        rp.CanEdit,
                        rp.CanDelete,
                        PermissionName = rp.Permission?.PermissionName
                    }) 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("STAFF_ACCOUNT_MANAGE", "add")]
        public async Task<JsonResult> CreateUser([FromBody] User user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.FullName))
                    return Json(new { success = false, message = "Họ tên không được để trống" });
                    
                if (user.FullName.Length > 100)
                    return Json(new { success = false, message = "Họ tên không được vượt quá 100 ký tự" });
                    
                if (string.IsNullOrWhiteSpace(user.Email))
                    return Json(new { success = false, message = "Email không được để trống" });
                
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không được để trống" });
                var phonePattern = new Regex(@"^[0-9]{10,15}$");
                var emailPattern = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");   

                if (!phonePattern.IsMatch(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại phải có 10-15 chữ số" });
                    
                if (!string.IsNullOrWhiteSpace(user.Email) && 
                    !emailPattern.IsMatch(user.Email))
                    return Json(new { success = false, message = "Email không đúng định dạng" });
                    
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                    return Json(new { success = false, message = "Mật khẩu không được để trống" });
                    
                // Salary validation
                if (user.Salary == null)
                    return Json(new { success = false, message = "Lương không được để trống" });
                if (user.Salary < 0)
                    return Json(new { success = false, message = "Lương không được nhỏ hơn 0" });
                if (user.Salary > 999999999)
                    return Json(new { success = false, message = "Lương không được vượt quá 999,999,999" });
                // Address validation
                if (!string.IsNullOrEmpty(user.Address) && user.Address.Length > 200)
                    return Json(new { success = false, message = "Địa chỉ không được vượt quá 200 ký tự" });
                if (user.PasswordHash.Length < 6)
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });

                var existingUserWithEmail = await _userService.GetUserByEmailAsync(user.Email);
                if (existingUserWithEmail != null)
                    return Json(new { success = false, message = "Email đã được sử dụng" });

                var existingUserWithPhone = await _userService.GetUserByPhoneAsync(user.PhoneNumber);
                if (existingUserWithPhone != null)
                    return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });

                user.PasswordHash = PasswordService.HashPassword(user.PasswordHash);
                await _userService.CreateUserAsync(user);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("STAFF_ACCOUNT_MANAGE", "edit")]
        public async Task<JsonResult> UpdateUser([FromBody] UserUpdateModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Dữ liệu không được để trống" });
                }
                
                _logger.LogInformation("Model: UpdatePassword={UpdatePassword}, NewPassword={NewPasswordLength}", 
                    model.UpdatePassword, 
                    model.NewPassword?.Length ?? 0);
                
                var user = model.User;
                if (user == null)
                {
                    return Json(new { success = false, message = "Dữ liệu người dùng không được để trống" });
                }
                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    return Json(new { success = false, message = "Họ tên không được để trống" });
                }
                    
                if (user.FullName.Length > 100)
                    return Json(new { success = false, message = "Họ tên không được vượt quá 100 ký tự" });
                    
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không được để trống" });
                
                if (string.IsNullOrWhiteSpace(user.Email))
                    return Json(new { success = false, message = "Email không được để trống" });
                
                var phonePattern = new Regex(@"^[0-9]{10,15}$");
                var emailPattern = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");   

                if (!phonePattern.IsMatch(user.PhoneNumber))
                    return Json(new { success = false, message = "Số điện thoại phải có 10-15 chữ số" });
                    
                if (!string.IsNullOrWhiteSpace(user.Email) && 
                    !emailPattern.IsMatch(user.Email))
                    return Json(new { success = false, message = "Email không đúng định dạng" });
                    
                if (!string.IsNullOrWhiteSpace(user.PasswordHash) && user.PasswordHash.Length < 6)
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });

                // Salary validation
                if (user.Salary == null)
                    return Json(new { success = false, message = "Lương không được để trống" });
                if (user.Salary < 0)
                    return Json(new { success = false, message = "Lương không được nhỏ hơn 0" });

                if (user.Salary > 999999999)
                    return Json(new { success = false, message = "Lương không được vượt quá 999,999,999" });
                // Address validation
                if (!string.IsNullOrEmpty(user.Address) && user.Address.Length > 200)
                    return Json(new { success = false, message = "Địa chỉ không được vượt quá 200 ký tự" });

                var existingUserWithEmail = await _userService.GetUserByEmailAsync(user.Email);
                // Kiểm tra nếu email đã tồn tại và không phải là người dùng hiện tại
                if (existingUserWithEmail != null && existingUserWithEmail.UserID != user.UserID)
                    return Json(new { success = false, message = "Email đã được sử dụng" });

                var existingUserWithPhone = await _userService.GetUserByPhoneAsync(user.PhoneNumber);
                // Kiểm tra nếu số điện thoại đã tồn tại và không phải là người dùng hiện tại
                if (existingUserWithPhone != null && existingUserWithPhone.UserID != user.UserID)
                    return Json(new { success = false, message = "Số điện thoại đã được sử dụng" });
                
                var existingUser = await _userService.GetUserByIdAsync(user.UserID);
                if (existingUser == null)
                {
                    _logger.LogWarning("Existing user not found with ID: {UserId}", user.UserID);
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }
                user.UserType = user.UserType == 0 ? existingUser.UserType : user.UserType;
                
                _logger.LogInformation("Password handling: UpdatePassword={UpdateFlag}, PasswordHash={HasPasswordHash}", 
                    model.UpdatePassword, 
                    !string.IsNullOrEmpty(user.PasswordHash));
                
                if (model.UpdatePassword)
                {
                    _logger.LogInformation("Cập nhật mật khẩu");
                    if (string.IsNullOrEmpty(model.NewPassword))
                    {
                        _logger.LogWarning("Mật khẩu mới trống");
                        return Json(new { success = false, message = "Mật khẩu mới không được để trống" });
                    }
                    
                    if (model.NewPassword.Length < 6)
                    {
                        _logger.LogWarning("Mật khẩu mới quá ngắn: {Length} ký tự", model.NewPassword.Length);
                        return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
                    }
                    
                    _logger.LogInformation("Mã hóa mật khẩu mới");
                    user.PasswordHash = PasswordService.HashPassword(model.NewPassword);
                }
                else
                {
                    if (string.IsNullOrEmpty(user.PasswordHash))
                    {
                        _logger.LogInformation("Mã băm mật khẩu trống, lấy dữ liệu người dùng hiện tại");
                        if (existingUser == null)
                        {
                            _logger.LogWarning("Không tìm thấy người dùng hiện tại với ID: {UserId}", user.UserID);
                            return Json(new { success = false, message = "Không tìm thấy người dùng" });
                        }
                        _logger.LogInformation("Sử dụng mã băm mật khẩu hiện tại");
                        user.PasswordHash = existingUser.PasswordHash;
                    }
                    else
                    {
                        _logger.LogInformation("Mã hóa mật khẩu đã cung cấp");
                        user.PasswordHash = PasswordService.HashPassword(user.PasswordHash);
                    }
                }
                
                _logger.LogInformation("Cập nhật người dùng trong cơ sở dữ liệu: {UserId}", user.UserID);
                await _userService.UpdateUserAsync(user);
                _logger.LogInformation("Người dùng được cập nhật thành công: {UserId}", user.UserID);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật người dùng");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "add")]
        public async Task<JsonResult> CreateRole([FromBody] Role role)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role.RoleKey))
                    return Json(new { success = false, message = "Key vai trò không được để trống" });
                    
                if (role.RoleKey.Length > 50)
                    return Json(new { success = false, message = "Key vai trò không được vượt quá 50 ký tự" });
                    
                if (string.IsNullOrWhiteSpace(role.RoleName))
                    return Json(new { success = false, message = "Tên vai trò không được để trống" });
                    
                if (role.RoleName.Length > 50)
                    return Json(new { success = false, message = "Tên vai trò không được vượt quá 50 ký tự" });
                    
                if (role.Description != null && role.Description.Length > 255)
                    return Json(new { success = false, message = "Mô tả không được vượt quá 255 ký tự" });

                var existingRole = await _userService.GetRoleByKeyAsync(role.RoleKey);
                if (existingRole != null)
                    return Json(new { success = false, message = "Vai trò đã tồn tại" });

                await _userService.CreateRoleAsync(role);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "edit")]
        public async Task<JsonResult> UpdateRole([FromBody] Role role)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role.RoleKey))
                    return Json(new { success = false, message = "Key vai trò không được để trống" });
                    
                if (role.RoleKey.Length > 50)
                    return Json(new { success = false, message = "Key vai trò không được vượt quá 50 ký tự" });
                    
                if (string.IsNullOrWhiteSpace(role.RoleName))
                    return Json(new { success = false, message = "Tên vai trò không được để trống" });
                    
                if (role.RoleName.Length > 50)
                    return Json(new { success = false, message = "Tên vai trò không được vượt quá 50 ký tự" });
                    
                if (role.Description != null && role.Description.Length > 255)
                    return Json(new { success = false, message = "Mô tả không được vượt quá 255 ký tự" });

                var existingRole = await _userService.GetRoleByKeyAsync(role.RoleKey);
                if (existingRole != null && existingRole.RoleID != role.RoleID)
                    return Json(new { success = false, message = "Vai trò đã tồn tại" });

                await _userService.UpdateRoleAsync(role);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("STAFF_ACCOUNT_MANAGE", "delete")]
        public async Task<JsonResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "delete")]
        public async Task<JsonResult> DeleteRole(int id)
        {
            try
            {
                await _userService.DeleteRoleAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ROLE_PERMISSION_MANAGE", "edit")]
        public async Task<JsonResult> UpdateRolePermissions([FromBody] RolePermissionsViewModel model)
        {
            try
            {
                var permissions = new Dictionary<int, RolePermission>();
                foreach (var perm in model.Permissions)
                {
                    permissions[perm.PermissionID] = new RolePermission
                    {
                        PermissionID = perm.PermissionID,
                        RoleID = model.RoleID,
                        CanView = perm.CanView,
                        CanAdd = perm.CanAdd,
                        CanEdit = perm.CanEdit,
                        CanDelete = perm.CanDelete
                    };
                }

                await _userService.UpdateRolePermissionsAsync(model.RoleID, permissions);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}