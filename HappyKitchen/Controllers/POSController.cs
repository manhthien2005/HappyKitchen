using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using HappyKitchen.Attributes;

namespace HappyKitchen.Controllers
{
    [AuthorizeAccess]
    public class PosController : Controller
    {
        private readonly IMenuItemService _menuItemService;
        private readonly ICategoryService _categoryService;
        private readonly IPosService _posService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PosController> _logger;

        public PosController(
            IMenuItemService menuItemService,
            ICategoryService categoryService,
            IPosService posService,
            ApplicationDbContext context,
            ILogger<PosController> logger)
        {
            _menuItemService = menuItemService;
            _categoryService = categoryService;
            _posService = posService;
            _context = context;
            _logger = logger;
        }

        [AuthorizeAccess("ORDER_PREPARE", "view")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            _logger.LogDebug("[API] GetTables");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var tables = await _posService.GetAllTablesAsync();
                stopwatch.Stop();
                _logger.LogDebug("GetTables completed in {ElapsedMs}ms, returned {Count} tables", stopwatch.ElapsedMilliseconds, tables.Count);
                return Json(new
                {
                    success = true,
                    data = tables.Select(t => new
                    {
                        t.TableID,
                        t.TableName,
                        t.Status
                    })
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in GetTables ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách bàn" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(string searchTerm = "")
        {
            _logger.LogDebug("[API] GetCustomers: searchTerm={SearchTerm}", searchTerm);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var customers = await _posService.GetCustomersAsync(searchTerm);
                stopwatch.Stop();
                _logger.LogDebug("GetCustomers completed in {ElapsedMs}ms, returned {Count} customers", stopwatch.ElapsedMilliseconds, customers.Count);
                return Json(new
                {
                    success = true,
                    data = customers.Take(10).Select(c => new
                    {
                        c.UserID,
                        c.FullName,
                        c.PhoneNumber
                    })
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in GetCustomers ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách khách hàng" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems(
            int page = 1,
            int pageSize = 9,
            string searchTerm = "",
            int categoryId = 0)
        {
            _logger.LogDebug("[API] GetMenuItems: page={Page}, size={Size}, search={Search}, category={Category}",
                page, pageSize, searchTerm, categoryId);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var menuItems = await _menuItemService.GetAllMenuItemsAsync();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    menuItems = menuItems.Where(m =>
                        m.Name.ToLower().Contains(searchTerm) ||
                        m.Description?.ToLower().Contains(searchTerm) == true
                    ).ToList();
                }
                if (categoryId > 0)
                {
                    menuItems = menuItems.Where(m => m.CategoryID == categoryId).ToList();
                }
                menuItems = menuItems.OrderBy(m => m.Name).ToList();
                int totalItems = menuItems.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                var pagedMenuItems = menuItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                stopwatch.Stop();
                _logger.LogDebug("GetMenuItems completed in {ElapsedMs}ms, returned {Count}/{Total} items",
                    stopwatch.ElapsedMilliseconds, pagedMenuItems.Count, totalItems);
                return Json(new
                {
                    success = true,
                    data = pagedMenuItems.Select(m => new
                    {
                        m.MenuItemID,
                        m.Name,
                        m.MenuItemImage,
                        CategoryName = m.Category?.CategoryName,
                        m.CategoryID,
                        m.Price,
                        m.Status
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in GetMenuItems ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách món ăn" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            _logger.LogDebug("[API] GetCategories");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                stopwatch.Stop();
                _logger.LogDebug("GetCategories completed in {ElapsedMs}ms, returned {Count} categories",
                    stopwatch.ElapsedMilliseconds, categories.Count);
                return Json(new
                {
                    success = true,
                    data = categories.Select(c => new
                    {
                        c.CategoryID,
                        c.CategoryName
                    })
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in GetCategories ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách danh mục" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ORDER_PREPARE", "add")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateModel model)
        {
            _logger.LogDebug("[API] CreateOrder: TableID={TableID}, CustomerID={CustomerID}, PaymentMethod={PaymentMethod}, Items={ItemCount}",
                model.TableID, model.CustomerID, model.PaymentMethod, model.OrderDetails?.Count ?? 0);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Validate session UserID
                var userIdString = HttpContext.Session.GetString("StaffID");
                _logger.LogDebug("Session StaffID={StaffID}", userIdString ?? "null");

                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogWarning("CreateOrder failed: StaffID is missing in session");
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                if (!int.TryParse(userIdString, out int employeeId))
                {
                    _logger.LogWarning("CreateOrder failed: Invalid StaffID format in session, StaffID={StaffID}", userIdString);
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                // Validate employee
                var employee = await _context.Users
                    .Where(u => u.UserID == employeeId && u.UserType == 1 && u.Status == 0)
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    _logger.LogWarning("CreateOrder failed: Employee with UserID {UserID} not found or inactive", employeeId);
                    return Json(new { success = false, message = "Nhân viên không hợp lệ hoặc không hoạt động." });
                }

                // Validate table
                if (model.TableID <= 0)
                {
                    _logger.LogWarning("CreateOrder failed: Invalid TableID={TableID}", model.TableID);
                    return Json(new { success = false, message = "Bàn không hợp lệ" });
                }

                // Validate order details
                if (model.OrderDetails == null || !model.OrderDetails.Any())
                {
                    _logger.LogWarning("CreateOrder failed: Order details are empty");
                    return Json(new { success = false, message = "Đơn hàng phải có ít nhất một món" });
                }

                // Validate table status
                var table = await _posService.GetTableByIdAsync(model.TableID);
                if (table == null)
                {
                    _logger.LogWarning("CreateOrder failed: TableID={TableID} does not exist", model.TableID);
                    return Json(new { success = false, message = "Bàn không tồn tại" });
                }

                // Kiểm tra trạng thái bàn
                if (table.Status == 1) // Đã đặt trước
                {
                    _logger.LogWarning("CreateOrder failed: TableID={TableID} is reserved", model.TableID);
                    return Json(new { success = false, message = "Bàn đã được đặt trước" });
                }
                if (table.Status == 2) // Đang sử dụng
                {
                    _logger.LogWarning("CreateOrder failed: TableID={TableID} is in use", model.TableID);
                    return Json(new { success = false, message = "Bàn đang được sử dụng" });
                }

                // Validate payment method
                var validPaymentMethods = new[] { 0, 1, 2 }; // 0: Chưa thanh toán, 1: Tiền mặt, 2: Thẻ
                if (!validPaymentMethods.Contains(model.PaymentMethod ?? 0))
                {
                    _logger.LogWarning("CreateOrder failed: Invalid PaymentMethod={PaymentMethod}", model.PaymentMethod);
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                }

                // Create order
                var order = new Order
                {
                    TableID = model.TableID,
                    CustomerID = model.CustomerID > 0 ? model.CustomerID : null,
                    EmployeeID = employeeId,
                    PaymentMethod = model.PaymentMethod ?? 0,
                    Status = 1, // Chờ xác nhận
                    OrderTime = DateTime.Now,
                    OrderDetails = model.OrderDetails.Select(od => new OrderDetail
                    {
                        MenuItemID = od.MenuItemID,
                        Quantity = od.Quantity,
                        Note = od.Note
                    }).ToList()
                };

                await _posService.CreateOrderAsync(order);
                stopwatch.Stop();
                _logger.LogInformation("Order created successfully in {ElapsedMs}ms: OrderID={OrderID}", stopwatch.ElapsedMilliseconds, order.OrderID);
                return Json(new { success = true, message = "Đơn hàng được tạo thành công" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in CreateOrder ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi tạo đơn hàng" });
            }
        }
    }

    public class OrderCreateModel
    {
        public int TableID { get; set; }
        public int? CustomerID { get; set; }
        public byte? PaymentMethod { get; set; }
        public List<OrderDetailModel> OrderDetails { get; set; }
    }

    public class OrderDetailModel
    {
        public int MenuItemID { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
    }
}