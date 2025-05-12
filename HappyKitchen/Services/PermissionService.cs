using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;
namespace HappyKitchen.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role) 
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionKey, string action)
        {
            var user = await GetUserAsync(userId);
            if (user == null || user.Role == null)
                return false;

            var rolePermission = user.Role.RolePermissions
                .FirstOrDefault(rp => rp.Permission.PermissionKey == permissionKey);

            if (rolePermission == null)
                return false;

            return action switch
            {
                "view" => rolePermission.CanView,
                "add" => rolePermission.CanAdd,
                "edit" => rolePermission.CanEdit,
                "delete" => rolePermission.CanDelete,
                _ => false
            };
        }

        public async Task<List<PermissionViewModel>> GetUserPermissionsAsync(int userId)
        {
            var user = await GetUserAsync(userId);
            if (user == null || user.Role == null)
                return new List<PermissionViewModel>();

            return user.Role.RolePermissions.Select(rp => new PermissionViewModel
            {
                PermissionID = rp.PermissionID,
                CanView = rp.CanView,
                CanAdd = rp.CanAdd,
                CanEdit = rp.CanEdit,
                CanDelete = rp.CanDelete
            }).ToList();
        }
    }
}