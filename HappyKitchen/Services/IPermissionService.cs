using HappyKitchen.Models;


namespace HappyKitchen.Services
{
    public interface IPermissionService
    {
        Task<User> GetUserAsync(int userId);
        Task<bool> HasPermissionAsync(int userId, string permissionKey, string action);
        Task<List<PermissionViewModel>> GetUserPermissionsAsync(int userId);
    }

}