using HappyKitchen.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace HappyKitchen.TagHelpers
{
    [HtmlTargetElement(Attributes = "permission")]
    [HtmlTargetElement(Attributes = "permission,action")]
    public class PermissionTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermissionService _permissionService;

        public string Permission { get; set; }
        public string Action { get; set; } = "view";

        public PermissionTagHelper(IHttpContextAccessor httpContextAccessor, IPermissionService permissionService)
        {
            _httpContextAccessor = httpContextAccessor;
            _permissionService = permissionService;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userIdString = httpContext.Session.GetString("StaffID");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                output.SuppressOutput();
                return;
            }

            bool hasPermission = await _permissionService.HasPermissionAsync(userId, Permission, Action);

            if (!hasPermission)
            {
                output.SuppressOutput();
            }
        }
    }
}