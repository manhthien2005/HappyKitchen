using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HappyKitchen.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(15)]
        public string PhoneNumber { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(255)]
        public string Address { get; set; }
    }

    public class Table
    {
        [Key]
        public int TableID { get; set; }

        [Required]
        public int TableNumber { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public byte Status { get; set; } // 0 = Trống, 1 = Đã đặt trước, 2 = Đang sử dụng
    }

    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(15)]
        public string PhoneNumber { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash {get; set; }

        [Required]
        public decimal Salary { get; set; }

        [Required]
        public byte Status { get; set; } // 0 = Đang làm, 1 = Nghỉ việc
    }

    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        [Required]
        public int CustomerID { get; set; }
        public Customer Customer { get; set; }

        [Required]
        public int TableID { get; set; }
        public Table Table { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedTime { get; set; }

        [Required]
        public DateTime ReservationTime { get; set; }

        [Required]
        public byte Status { get; set; } // 0 = Đã hủy, 1 = Đang chờ, 2 = Đã xác nhận
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
        public Category Category { get; set; }

        [Required]
        public decimal Price { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [Required]
        public byte Status { get; set; } // 0 = Hết hàng, 1 = Còn hàng

        // Quan hệ 1-N với MenuItemAttribute
        public ICollection<MenuItemAttribute> Attributes { get; set; } = new List<MenuItemAttribute>();
    }

    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int CustomerID { get; set; }
        public Customer Customer { get; set; }

        [Required]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }

        [Required]
        public int TableID { get; set; }
        public Table Table { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime OrderTime { get; set; }

        [Required]
        public byte Status { get; set; } // 0 = Đã hủy, 1 = Chờ xác nhận, 2 = Đang chuẩn bị, 3 = Hoàn thành

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; }
    }

    public class OrderDetail
    {
        [Key]
        public int OrderDetailID { get; set; }

        [Required]
        public int OrderID { get; set; }
        public Order Order { get; set; }

        [Required]
        public int MenuItemID { get; set; }
        public MenuItem MenuItem { get; set; }

        [Required]
        public int Quantity { get; set; }
    }

    public class MenuItemAttribute
    {
        [Key]
        public int AttributeID { get; set; }

        [Required]
        public int MenuItemID { get; set; } // Liên kết với MenuItem

        [Required, MaxLength(100)]
        public string AttributeName { get; set; } // Tên thuộc tính (VD: "Spiciness", "Calories")

        [Required, MaxLength(255)]
        public string AttributeValue { get; set; } // Giá trị của thuộc tính (VD: "Medium", "350 kcal")

        // Quan hệ với MenuItem
        public MenuItem MenuItem { get; set; }
    }

    public class EmployeeLogin
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

    public class OTPModel
    {
        public string OTPCode { get; set; }
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



}
