using HappyKitchen.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace HappyKitchen.Helpers
{
    public static class AdminAuthHelper
    {
        public static async Task<bool> IsAdminUser(this IServiceProvider services, HttpContext httpContext)
        {
            var userIdString = httpContext.Session.GetString("StaffID");
            
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return false;
            }
            
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            
            return user != null && user.UserType == 1;
        }
    }
}