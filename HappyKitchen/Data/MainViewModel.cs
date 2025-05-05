using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HappyKitchen.Models
{

    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(15)]
        [Phone]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public ICollection<MenuItemRating> Ratings { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "UserType must be 0 (Customer) or 1 (Employee)")]
        public byte UserType { get; set; } // 0 = Khách hàng, 1 = Nhân viên

        public string PasswordHash { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Salary { get; set; } // Chỉ áp dụng cho nhân viên

        [Required]
        [Range(0, 2, ErrorMessage = "Status must be 0 (Active), 1 (Blocked), or 2 (Resigned)")]
        public byte Status { get; set; } = 0; // 0 = Hoạt động, 1 = Bị khóa, 2 = Nghỉ việc
    
        public int? RoleID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role? Role { get; set; }
        [InverseProperty("Customer")]
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
    public class Table
    {
        [Key]
        public int TableID { get; set; }

        [Required]
        [MaxLength(50)]
        public string TableName { get; set; } = string.Empty; 

        [Required]
        public int AreaID { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        [Range(0, 2)]
        public byte Status { get; set; }

        [ForeignKey("AreaID")]
        public virtual Area Area { get; set; }
    }
    public class Area
    {
        [Key]
        public int AreaID { get; set; }

        [Required]
        [StringLength(50)]
        public string AreaName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<Table>? Tables { get; set; }
    }
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        public int TableID { get; set; }

        [Required]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        [Required]
        public DateTime ReservationTime { get; set; }

        [Required]
        [Range(0, 2, ErrorMessage = "Status must be 0 (Canceled), 1 (Pending), or 2 (Confirmed)")]
        public byte Status { get; set; }

        [StringLength(255)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual User Customer { get; set; }

        [ForeignKey("TableID")]
        public virtual Table Table { get; set; }
    }

    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required, MaxLength(50)]
        public string CategoryName { get; set; }

        // Thiết lập quan hệ 1-N: Một Category có nhiều MenuItem
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }

    public class MenuItem
    {
        [Key]
        public int MenuItemID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(255)]
        public string MenuItemImage { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
        public decimal Price { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public byte Status { get; set; } // 0 = Hết hàng, 1 = Còn hàng

        public virtual ICollection<MenuItemAttribute> Attributes { get; set; } = new List<MenuItemAttribute>();
        public virtual ICollection<MenuItemRating> Ratings { get; set; } = new List<MenuItemRating>();

        [NotMapped]
        public double AverageRating => Ratings?.Any() == true ? Ratings.Average(r => r.Rating) : 0;

        [NotMapped]
        public int RatingCount => Ratings?.Count ?? 0;

        [NotMapped]
        public int TotalComments => Ratings?.Count(r => !string.IsNullOrEmpty(r.Comment)) ?? 0;
    }

    public class MenuItemAttribute
    {
        [Key]
        public int AttributeID { get; set; }

        [Required]
        public int MenuItemID { get; set; }

        [Required, MaxLength(100)]
        public string AttributeName { get; set; }

        [Required, MaxLength(255)]
        public string AttributeValue { get; set; }

        [ForeignKey("MenuItemID")]
        public virtual MenuItem MenuItem { get; set; }
    }


    public class MenuItemRating
    {
        [Key]
        public int RatingID { get; set; }

        [Required]
        public int MenuItemID { get; set; }

        [ForeignKey("MenuItemID")]
        public MenuItem MenuItem { get; set; }

        [Required]
        public int UserID { get; set; } // Liên kết với User

        [ForeignKey("UserID")]
        public User User { get; set; }

        [Required]
        [Range(1, 5)]
        public byte Rating { get; set; } // Rating từ 1 đến 5 sao

        [MaxLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        public int? CustomerID { get; set; }

        public int? EmployeeID { get; set; }

        [Required]
        public int TableID { get; set; }

        [Required]
        public DateTime OrderTime { get; set; } = DateTime.Now;

        [Required]
        [Range(0, 3, ErrorMessage = "Status must be 0 (Canceled), 1 (Pending Confirmation), 2 (Preparing), or 3 (Completed)")]
        public byte Status { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual User? Customer { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual User? Employee { get; set; }
        [ForeignKey("TableID")]
        public virtual Table Table { get; set; }
        [InverseProperty("Order")]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public class OrderDetail
    {
        [Key]
        public int OrderDetailID { get; set; }

        [Required]
        [ForeignKey("Order")]
        public int OrderID { get; set; }

        [Required]
        [ForeignKey("MenuItem")]
        public int MenuItemID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        [StringLength(200)]
        public string Note { get; set; }

        public virtual Order Order { get; set; }
        public virtual MenuItem MenuItem { get; set; }
    }

    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        public int MenuItemID { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "StarRating must be between 1 and 5")]
        public byte StarRating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        [Required]
        public DateTime ReviewTime { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual User Customer { get; set; }

        [ForeignKey("MenuItemID")]
        public virtual MenuItem MenuItem { get; set; }
    }

    public class EmployeeLogin
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string RecaptchaToken { get; set; }
        public bool RememberMe { get; set; }
    }

    public class UserLogin
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string RecaptchaToken { get; set; }
    }

    public class EmployeeRegister
    {
        [Required, MaxLength(60)]
        public string FullName { get; set; }

        [Required, MaxLength(10)]
        public string PhoneNumber { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required, Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    public class UserRegister
    {
        [Required, MaxLength(60)]
        public string FullName { get; set; }

        [Required, MaxLength(10)]
        public string PhoneNumber { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required, Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    public class OTPModel
    {
        public string OTPCode { get; set; }
        public bool RememberMe { get; set; }
    }

    public class OTPPasswordModel
    {
        public string OTPPassCode { get; set; }
    }

    public class ResetPasswordModel
    {
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    public class TrustedDevice
    {
        public int Id { get; set; }
        public int UserID { get; set; } // ID của người dùng
        public string DeviceToken { get; set; } // Token định danh duy nhất cho thiết bị
        public DateTime CreatedAt { get; set; }
    }

    public class CommentRequestModel
    {
        public int MenuItemId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class QRCode
    {
        [Key]
        public int QRCodeID { get; set; }

        [Required]
        [ForeignKey(nameof(Table))] 
        public int TableID { get; set; }

        [Required]
        [MaxLength(255)]
        public string QRCodeImage { get; set; } = string.Empty; 

        [Required]
        [MaxLength(500)]
        public string MenuUrl { get; set; } = string.Empty;

        public int AccessCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (Inactive) or 1 (Active)")]
        public byte Status { get; set; } = 1;

        public virtual Table Table { get; set; }
    }

}
