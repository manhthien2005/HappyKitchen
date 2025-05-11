using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace HappyKitchen.Services
{
    public interface IPermissionService
    {
        Task<User> GetUserAsync(int userId);
        Task<bool> HasPermissionAsync(int userId, string permissionKey, string action);
        Task<List<PermissionViewModel>> GetUserPermissionsAsync(int userId);
    }

}