using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HappyKitchen.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleKey { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [JsonIgnore]
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        [JsonIgnore]
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class Permission
    {
        [Key]
        public int PermissionID { get; set; }

        [Required]
        [StringLength(50)]
        public string PermissionKey { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {

        [Required]
        public int RoleID { get; set; }

        [Required]
        public int PermissionID { get; set; }

        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("PermissionID")]
        public virtual Permission Permission { get; set; } = null!;
    }

    
    public class RolePermissionsViewModel
    {
        public int RoleID { get; set; }
        public List<PermissionViewModel> Permissions { get; set; }
    }

    public class PermissionViewModel
    {
        public int PermissionID { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
    public class UserUpdateModel
    {
        public User User { get; set; }
        public bool UpdatePassword { get; set; }
        public string? NewPassword { get; set; }
    }

}