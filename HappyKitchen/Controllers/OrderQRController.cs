using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HappyKitchen.Controllers
{
    public class OrderQRController : Controller
    {
        private readonly ITableService _tableService;
        private readonly IMenuItemService _menuItemService;
        private readonly ICategoryService _categoryService;
        private readonly IPosService _posService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderQRController> _logger;

        public OrderQRController(
            ITableService tableService,
            IMenuItemService menuItemService,
            ICategoryService categoryService,
            IPosService posService,
            ApplicationDbContext context,
            ILogger<OrderQRController> logger)
        {
            _tableService = tableService;
            _menuItemService = menuItemService;
            _categoryService = categoryService;
            _posService = posService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int tableId)
        {
            _logger.LogDebug("[View] Index: TableID={TableID}", tableId);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (tableId <= 0)
                {
                    _logger.LogWarning("Index: Invalid TableID={TableID}", tableId);
                    return View("Error", new ErrorViewModel { Message = "Bàn không hợp lệ" });
                }

                var table = await _posService.GetTableByIdAsync(tableId);
                if (table == null)
                {
                    _logger.LogWarning("Index: TableID={TableID} does not exist", tableId);
                    return View("Error", new ErrorViewModel { Message = "Bàn không tồn tại" });
                }

                if (table.Status == 2)
                {
                    _logger.LogWarning("Index: TableID={TableID} is in use", tableId);
                    return View("Error", new ErrorViewModel { Message = "Bàn đang được sử dụng" });
                }

                if (table.Status == 1)
                {
                    _logger.LogWarning("Index: TableID={TableID} is reserved", tableId);
                    return View("Error", new ErrorViewModel { Message = "Bàn đã được đặt trước" });
                }

                // Tăng số lượt truy cập QR code
                await TrackQRCodeAccessByTableId(tableId);

                ViewBag.TableId = tableId;
                ViewBag.TableName = table.TableName;
                ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "-";
                ViewBag.Phone = HttpContext.Session.GetString("Phone") ?? "-"; 
                ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserID").HasValue;
                stopwatch.Stop();
                _logger.LogDebug("Index completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return View("Index");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in Index ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return View("Error", new ErrorViewModel { Message = "Đã xảy ra lỗi, vui lòng thử lại sau" });
            }
        }
        
        // Phương thức mới để theo dõi lượt truy cập QR code theo tableId
        private async Task TrackQRCodeAccessByTableId(int tableId)
        {
            try
            {
                // Lấy QR code từ tableId
                var qrCode = await _context.QRCodes
                    .FirstOrDefaultAsync(q => q.TableID == tableId && q.Status == 0);
                
                if (qrCode != null)
                {
                    // Tăng số lượt truy cập
                    qrCode.AccessCount++;
                    _context.Entry(qrCode).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("QR Code access tracked: TableID={TableID}, QRCodeID={QRCodeID}, AccessCount={AccessCount}", 
                        tableId, qrCode.QRCodeID, qrCode.AccessCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking QR code access for TableID={TableID}: {Message}", tableId, ex.Message);
                // Không ném ngoại lệ để không ảnh hưởng đến luồng chính
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMenuItems(int page = 1, int pageSize = 8, string searchTerm = "", int categoryId = 0)
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
                return Json(new { success = false, message = "Lỗi khi lấy danh mục món ăn" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] QROrderCreateModel model)
        {
            _logger.LogDebug("[API] CreateOrder: TableID={TableID}, Items={ItemCount}", model.TableID, model.OrderDetails?.Count ?? 0);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get UserID from session
                int? customerId = null;
                var userIdValue = HttpContext.Session.GetInt32("UserID");
                if (userIdValue.HasValue)
                {
                    var user = await _context.Users
                        .Where(u => u.UserID == userIdValue.Value && u.Status == 0)
                        .FirstOrDefaultAsync();
                    if (user != null)
                    {
                        customerId = user.UserID;
                    }
                    else
                    {
                        _logger.LogWarning("CreateOrder: UserID={UserID} not found or inactive", userIdValue.Value);
                    }
                }

                // Validate table
                if (model.TableID <= 0)
                {
                    _logger.LogWarning("CreateOrder failed: Invalid TableID={TableID}", model.TableID);
                    return Json(new { success = false, message = "Bàn không hợp lệ" });
                }

                var table = await _posService.GetTableByIdAsync(model.TableID);
                if (table == null)
                {
                    _logger.LogWarning("CreateOrder failed: TableID={TableID} does not exist", model.TableID);
                    return Json(new { success = false, message = "Bàn không tồn tại" });
                }

                if (table.Status == 2)
                {
                    _logger.LogWarning("CreateOrder failed: TableID={TableID} is in use", model.TableID);
                    return Json(new { success = false, message = "Bàn đang được sử dụng" });
                }

                if (table.Status == 1)
                {
                    _logger.LogWarning("CreateOrder failed: TableID={TableID} is reserved", model.TableID);
                    return Json(new { success = false, message = "Bàn đã được đặt trước" });
                }

                // Validate order details
                if (model.OrderDetails == null || !model.OrderDetails.Any())
                {
                    _logger.LogWarning("CreateOrder failed: Order details are empty");
                    return Json(new { success = false, message = "Đơn hàng phải có ít nhất một món" });
                }

                // Create order
                var order = new Order
                {
                    TableID = model.TableID,
                    CustomerID = customerId,
                    EmployeeID = null,
                    PaymentMethod = 0,
                    Status = 1,
                    OrderTime = DateTime.Now,
                    OrderDetails = model.OrderDetails.Select(od => new OrderDetail
                    {
                        MenuItemID = od.MenuItemID,
                        Quantity = od.Quantity,
                        Note = od.Note
                    }).ToList()
                };

                // Update table status
                table.Status = 2;
                await _tableService.UpdateTableAsync(table);

                await _posService.CreateOrderAsync(order);
                stopwatch.Stop();
                _logger.LogInformation("Order created successfully in {ElapsedMs}ms: OrderID={OrderID}, CustomerID={CustomerID}",
                    stopwatch.ElapsedMilliseconds, order.OrderID, customerId ?? null);
                return Json(new { success = true, message = "Đơn hàng được tạo thành công" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in CreateOrder ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi tạo đơn hàng" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateTable(int tableId)
        {
            _logger.LogDebug("[API] ValidateTable: TableID={TableID}", tableId);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (tableId <= 0)
                {
                    _logger.LogWarning("ValidateTable failed: Invalid TableID={TableID}", tableId);
                    return Json(new { success = false, message = "Bàn không hợp lệ" });
                }

                var table = await _posService.GetTableByIdAsync(tableId);
                if (table == null)
                {
                    _logger.LogWarning("ValidateTable failed: TableID={TableID} does not exist", tableId);
                    return Json(new { success = false, message = "Bàn không tồn tại" });
                }

                if (table.Status == 2)
                {
                    _logger.LogWarning("ValidateTable failed: TableID={TableID} is in use", tableId);
                    return Json(new { success = false, message = "Bàn đang được sử dụng" });
                }

                if (table.Status == 1)
                {
                    _logger.LogWarning("ValidateTable failed: TableID={TableID} is reserved", tableId);
                    return Json(new { success = false, message = "Bàn đã được đặt trước" });
                }

                // Lấy thông tin từ session
                string fullName = HttpContext.Session.GetString("FullName") ?? "Khách QR";
                string phone = HttpContext.Session.GetString("Phone") ?? "-";

                stopwatch.Stop();
                _logger.LogDebug("ValidateTable completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return Json(new { 
                    success = true, 
                    data = new { 
                        table.TableID, 
                        table.TableName, 
                        table.Capacity,
                        FullName = fullName,
                        Phone = phone
                    } 
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in ValidateTable ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi kiểm tra trạng thái bàn" });
            }
        }
        public class ErrorViewModel
        {
            public string Message { get; set; }
        }
        public class QROrderCreateModel
        {
            public int TableID { get; set; }
            public List<QROrderDetailModel> OrderDetails { get; set; }
        }
        public class QROrderDetailModel
        {
            public int MenuItemID { get; set; }
            public int Quantity { get; set; }
            public string Note { get; set; }
        }

    }
}