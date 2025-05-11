using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace HappyKitchen.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeAccessAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string? _permissionKey;
        private readonly string? _action;

        public AuthorizeAccessAttribute()
        {
            _permissionKey = null;
            _action = null;
        }

        public AuthorizeAccessAttribute(string permissionKey, string action)
        {
            _permissionKey = permissionKey;
            _action = action;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if we're already on the Login page to prevent redirect loops
            if (context.HttpContext.Request.Path.StartsWithSegments("/Admin/Login"))
            {
                return; // Skip authorization for the Login page itself
            }
            
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userIdString = context.HttpContext.Session.GetString("StaffID");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                HandleUnauthorized(context, "User not logged in");
                return;
            }

            var user = await permissionService.GetUserAsync(userId);
            if (user == null || user.UserType != 1) // Only admins (UserType = 1)
            {
                HandleUnauthorized(context, "Admin access required");
                return;
            }

            if (_permissionKey != null && _action != null)
            {
                bool hasPermission = await permissionService.HasPermissionAsync(userId, _permissionKey, _action);
                if (!hasPermission)
                {
                    HandleUnauthorized(context, "Permission denied");
                    return;
                }
            }
        }

        private void HandleUnauthorized(AuthorizationFilterContext context, string message)
        {
            if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Result = new JsonResult(new { success = false, message })
                {
                    StatusCode = 403
                };
            }
            else
            {
                context.Result = _permissionKey == null
                    ? new RedirectToActionResult("Login", "Admin", null)
                    : new ViewResult { ViewName = "AccessDenied" };
            }
        }
    }
}