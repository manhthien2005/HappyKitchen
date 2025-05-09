
using HappyKitchen.Models;

namespace HappyKitchen.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync(int userType);
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByPhoneAsync(string phone);
        Task<List<User>> SearchUsersAsync(string query);
        Task<List<User>> GetUsersByRoleAsync(int roleId);
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
    
        Task<List<Role>> GetAllRolesAsync();
        Task<Role> GetRoleByIdAsync(int id);
        Task<Role> GetRoleByKeyAsync(string roleName);
        Task CreateRoleAsync(Role role);
        Task UpdateRoleAsync(Role role);
        Task DeleteRoleAsync(int id);
    
        Task<List<Permission>> GetAllPermissionsAsync();
        Task AssignRoleToUserAsync(int userId, int roleId);
        Task UpdateRolePermissionsAsync(int roleId, Dictionary<int, RolePermission> permissions);
        Task<List<Order>> GetOrdersByUserAsync(int userId); 
        Task<Order> GetOrderByIdAsync(int id); 
    }
}