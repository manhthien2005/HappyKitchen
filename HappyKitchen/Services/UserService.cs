using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyKitchen.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        // SELECT u.*, r.*
        // FROM Users u
        // LEFT JOIN Roles r ON u.RoleId = r.Id
        public async Task<List<User>> GetAllUsersAsync(int userType)
        {
            try
            {
                if (userType == 0)
                {
                    return await _context.Users
                        .Include(u => u.Orders)
                            .ThenInclude(o => o.OrderDetails)
                                .ThenInclude(od => od.MenuItem)
                        .Where(u => u.UserType == userType)
                        .AsNoTracking()
                        .Select(u => new User
                        {
                            UserID = u.UserID,
                            FullName = u.FullName,
                            Email = u.Email,
                            PhoneNumber = u.PhoneNumber,
                            Address = u.Address,
                            UserType = u.UserType,
                            Status = u.Status,
                            Orders = u.Orders.Select(o => new Order
                            {
                                OrderID = o.OrderID,
                                OrderTime = o.OrderTime,
                                Status = o.Status,
                                PaymentMethod = o.PaymentMethod,
                                OrderDetails = o.OrderDetails.Select(od => new OrderDetail
                                {
                                    OrderDetailID = od.OrderDetailID,
                                    Quantity = od.Quantity,
                                    MenuItem = new MenuItem
                                    {
                                        MenuItemID = od.MenuItem.MenuItemID,
                                        Name = od.MenuItem.Name,
                                        Price = od.MenuItem.Price
                                    }
                                }).ToList()
                            }).ToList()
                        })
                        .ToListAsync();
                }
                else
                {
                    return await _context.Users
                       .Include(u => u.Role)
                       .Where(u => u.UserType == userType)
                       .AsNoTracking()
                       .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllUsersAsync: {ex.Message}");
                throw;
            }
        }

        public async Task CreateUserAsync(User user)
        {
            // Hash password if provided
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.UserID);
            if (existingUser == null)
                throw new KeyNotFoundException($"User with ID {user.UserID} not found");

            // Only update password if a new one is provided
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.Address = user.Address;
            existingUser.Status = user.Status;
            existingUser.RoleID = user.RoleID;
            existingUser.UserType = user.UserType;
            existingUser.Salary = user.Salary;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {id} not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }


        // SELECT r.*, rp.*, p.*, u.*
        // FROM Roles r
        // LEFT JOIN RolePermissions rp ON r.RoleID = rp.RoleID
        // LEFT JOIN Permissions p ON rp.PermissionId = p.PermissionID
        // LEFT JOIN Users u ON u.RoleID = r.RoleID
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Include(r => r.Users)
                .AsNoTracking() 
                .ToListAsync();
        }

        public async Task<Role> GetRoleByIdAsync(int id)
        {
            return await _context.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleID == id);
        }

        public async Task CreateRoleAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoleAsync(Role role)
        {
            var existingRole = await _context.Roles.FindAsync(role.RoleID);
            if (existingRole == null)
                throw new KeyNotFoundException($"Role with ID {role.RoleID} not found");

            existingRole.RoleName = role.RoleName;
            existingRole.RoleKey = role.RoleKey;
            existingRole.Description = role.Description;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleID == id);

            if (role == null)
                throw new KeyNotFoundException($"Role with ID {id} not found");

            if (role.Users.Any())
                throw new InvalidOperationException("Cannot delete role with assigned users");

            // Delete role permissions first
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleID == id)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(rolePermissions);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .Include(p => p.RolePermissions)
                .ToListAsync();
        }

        public async Task AssignRoleToUserAsync(int userId, int roleId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found");

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role with ID {roleId} not found");

            user.RoleID = roleId;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRolePermissionsAsync(int roleId, Dictionary<int, RolePermission> permissions)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleID == roleId);

            if (role == null)
                throw new KeyNotFoundException($"Role with ID {roleId} not found");

            // Remove existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);
            await _context.SaveChangesAsync();

            // Add new permissions
            foreach (var permission in permissions.Values)
            {
                permission.RoleID = roleId;
                _context.RolePermissions.Add(permission);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == id);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserByPhoneAsync(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return null;

            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }
        public async Task<Role> GetRoleByKeyAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return null;

            return await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleKey == roleName);
        }

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
                return await GetAllUsersAsync(1);

            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.UserType == 1 && (u.FullName.Contains(query) ||
                           (u.Email != null && u.Email.Contains(query)) ||
                           (u.PhoneNumber != null && u.PhoneNumber.Contains(query))))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByRoleAsync(int roleId)

        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleID == roleId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<List<Order>> GetOrdersByUserAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Where(o => o.CustomerID == userId || o.EmployeeID == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
        }
    }
}
