using HappyKitchen.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace HappyKitchen.Helpers
{
    public static class PermissionHelper
    {
        public static async Task<bool> HasPermission(this IServiceProvider services, HttpContext httpContext, string permissionKey, string action)
        {
            var permissionService = services.GetRequiredService<IPermissionService>();
            var userIdString = httpContext.Session.GetString("StaffID");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return false;
            }

            return await permissionService.HasPermissionAsync(userId, permissionKey, action);
        }

        public static JsonResult CreatePermissionDeniedResult(string message = "Bạn không có quyền thực hiện hành động này")
        {
            return new JsonResult(new { success = false, message = message })
            {
                StatusCode = 403
            };
        }
    }
}