using HappyKitchen.Attributes;
using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace HappyKitchen.Controllers
{
    public class OrderManageController : Controller
    {
        private readonly IPosService _posService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderManageController> _logger;

        public OrderManageController(
            IPosService posService,
            ApplicationDbContext context,
            ILogger<OrderManageController> logger)
        {
            _posService = posService;
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeAccess("ORDER_MANAGE", "view")]
        public async Task<IActionResult> GetOrders(
            int page = 1,
            int pageSize = 8,
            string searchTerm = "",
            string status = "all",
            string startDate = "",
            string endDate = "",
            bool searchInDetails = false)
        {
            _logger.LogDebug("[API] GetOrders: page={Page}, size={Size}, search={Search}, status={Status}, startDate={StartDate}, endDate={EndDate}, searchInDetails={SearchInDetails}",
                page, pageSize, searchTerm, status, startDate, endDate, searchInDetails);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Customer)
                    .Include(o => o.Employee)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.MenuItem)
                    .ToListAsync();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    orders = orders.Where(o =>
                        o.OrderID.ToString().Contains(searchTerm) ||
                        o.Table.TableName.ToLower().Contains(searchTerm) ||
                        (o.Customer != null && o.Customer.FullName.ToLower().Contains(searchTerm)) ||
                        (searchInDetails && o.OrderDetails.Any(od =>
                            od.MenuItem.Name.ToLower().Contains(searchTerm) ||
                            (od.Note != null && od.Note.ToLower().Contains(searchTerm))))
                    ).ToList();
                }

                if (status != "all")
                {
                    byte statusValue = byte.Parse(status);
                    orders = orders.Where(o => o.Status == statusValue).ToList();
                }

                if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out DateTime start))
                {
                    orders = orders.Where(o => o.OrderTime >= start).ToList();
                }

                if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out DateTime end))
                {
                    orders = orders.Where(o => o.OrderTime <= end.AddDays(1).AddTicks(-1)).ToList();
                }

                // Sort by OrderTime descending
                orders = orders.OrderByDescending(o => o.OrderTime).ToList();

                // Pagination
                int totalItems = orders.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var pagedOrders = orders
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                stopwatch.Stop();
                _logger.LogDebug("GetOrders completed in {ElapsedMs}ms, returned {Count}/{Total} orders",
                    stopwatch.ElapsedMilliseconds, pagedOrders.Count, totalItems);

                return Json(new
                {
                    success = true,
                    data = pagedOrders.Select(o => new
                    {
                        o.OrderID,
                        TableName = o.Table?.TableName,
                        CustomerName = o.Customer?.FullName ?? "Khách vãng lai",
                        EmployeeName = o.Employee?.FullName,
                        o.OrderTime,
                        o.Status,
                        o.PaymentMethod,
                        Total = o.OrderDetails.Sum(od => od.Quantity * od.MenuItem.Price),
                        OrderDetails = o.OrderDetails.Select(od => new
                        {
                            od.OrderDetailID,
                            od.MenuItemID,
                            MenuItemName = od.MenuItem.Name,
                            od.Quantity,
                            od.Note,
                            UnitPrice = od.MenuItem.Price
                        })
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
                _logger.LogError(ex, "Error in GetOrders ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách đơn hàng" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ORDER_MANAGE", "edit")]
        public async Task<IActionResult> UpdateOrder([FromBody] OrderUpdateModel model)
        {
            _logger.LogDebug("[API] UpdateOrder: OrderID={OrderID}, Status={Status}, PaymentMethod={PaymentMethod}, ItemCount={ItemCount}",
                model.OrderID, model.Status, model.PaymentMethod, model.OrderDetails?.Count ?? 0);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var userIdString = HttpContext.Session.GetString("StaffID");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int employeeId))
                {
                    _logger.LogWarning("UpdateOrder failed: Invalid or missing UserID in session");
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                var employee = await _context.Users
                    .Where(u => u.UserID == employeeId && u.UserType == 1 && u.Status == 1)
                    .FirstOrDefaultAsync();
                if (employee == null)
                {
                    _logger.LogWarning("UpdateOrder failed: Employee with UserID {UserID} not found or inactive", employeeId);
                    return Json(new { success = false, message = "Nhân viên không hợp lệ hoặc không hoạt động." });
                }

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderID == model.OrderID);
                if (order == null)
                {
                    _logger.LogWarning("UpdateOrder failed: Order with ID {OrderID} not found", model.OrderID);
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // if (!Enum.IsDefined(typeof(OrderStatus), model.Status))
                // {
                //     return Json(new { success = false, message = "Trạng thái đơn hàng không hợp lệ" });
                // }

                // if (model.PaymentMethod == 0)
                // {
                //     return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                // }

                order.Status = model.Status;
                order.PaymentMethod = model.PaymentMethod;

                if (model.OrderDetails != null && model.OrderDetails.Any())
                {
                    _context.OrderDetails.RemoveRange(order.OrderDetails);
                    await _context.SaveChangesAsync();

                    order.OrderDetails = model.OrderDetails.Select(od => new OrderDetail
                    {
                        MenuItemID = od.MenuItemID,
                        Quantity = od.Quantity,
                        Note = od.Note
                    }).ToList();
                }

                _context.Entry(order).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation("Update order successful in {ElapsedMs}ms: OrderID={OrderID}",
                    stopwatch.ElapsedMilliseconds, order.OrderID);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in UpdateOrder ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi cập nhật đơn hàng" });
            }
        }

        [HttpPost]
        [AuthorizeAccess("ORDER_MANAGE", "delete")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            _logger.LogDebug("[API] DeleteOrder: OrderID={OrderID}", id);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderID == id);
                if (order == null)
                {
                    _logger.LogWarning("DeleteOrder failed: Order with ID {OrderID} not found", id);
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation("Order deleted successfully in {ElapsedMs}ms: OrderID={OrderID}",
                    stopwatch.ElapsedMilliseconds, id);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in DeleteOrder ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi xóa đơn hàng" });
            }
        }
    }

    // public enum OrderStatus : byte
    // {
    //     Canceled = 0,
    //     PendingConfirmation = 1,
    //     Preparing = 2,
    //     Completed = 3
    // }

    public class OrderUpdateModel
    {
        public int OrderID { get; set; }
        public byte Status { get; set; }
        public byte PaymentMethod { get; set; }
        public List<OrderDetailModel> OrderDetails { get; set; }
    }

}