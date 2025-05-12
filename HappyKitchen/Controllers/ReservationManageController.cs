using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HappyKitchen.Controllers
{
    public class ReservationManageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationManageController> _logger;

        public ReservationManageController(
        ApplicationDbContext context,
        ILogger<ReservationManageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetReservations(
        int page = 1,
        int pageSize = 8,
        string searchTerm = "",
        string status = "all",
        string startDate = "",
        string endDate = "",
        bool searchInDetails = false)
        {
            _logger.LogDebug("[API] GetReservations: page={Page}, size={Size}, search={Search}, status={Status}, startDate={StartDate}, endDate={EndDate}, searchInDetails={SearchInDetails}",
            page, pageSize, searchTerm, status, startDate, endDate, searchInDetails);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var query = _context.Reservations
                .Include(r => r.Table)
                .Include(r => r.Customer)
                .Include(r => r.Orders)
                .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(r =>
                    r.ReservationID.ToString().Contains(searchTerm) ||
                    r.CustomerName.ToLower().Contains(searchTerm) ||
                    r.Table.TableName.ToLower().Contains(searchTerm));
                }

                if (status != "all" && byte.TryParse(status, out byte statusValue))
                {
                    query = query.Where(r => r.Status == statusValue);
                }

                if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out DateTime start))
                {
                    query = query.Where(r => r.ReservationTime >= start);
                }

                if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out DateTime end))
                {
                    query = query.Where(r => r.ReservationTime <= end.AddDays(1).AddTicks(-1));
                }

                // Count total items
                int totalItems = await query.CountAsync();

                // Apply sorting and pagination
                var pagedReservations = await query
                .OrderByDescending(r => r.ReservationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                stopwatch.Stop();
                _logger.LogDebug("GetReservations completed in {ElapsedMs}ms, returned {Count}/{Total} reservations",
                stopwatch.ElapsedMilliseconds, pagedReservations.Count, totalItems);

                return Json(new
                {
                    success = true,
                    data = pagedReservations.Select(r => new
                    {
                        r.ReservationID,
                        r.CustomerName,
                        ReservationTime = r.ReservationTime.ToString("o"),
                        r.Capacity,
                        r.Status,
                        TableName = r.Table?.TableName,
                        CustomerPhone = r.CustomerPhone,
                        Duration = r.Duration,
                        CreatedTime = r.CreatedTime.ToString("o"),
                        Notes = r.Notes,
                        OrderID = r.OrderID
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
                _logger.LogError(ex, "Error in GetReservations ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lấy danh sách đơn đặt bàn" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReservation([FromBody] ReservationUpdateModel model)
        {
            _logger.LogDebug("[API] UpdateReservation: ReservationID={ReservationID}, Status={Status}, Capacity={Capacity}", model.ReservationID, model.Status, model.Capacity);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var userIdString = HttpContext.Session.GetString("StaffID");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int employeeId))
                {
                    _logger.LogWarning("UpdateReservation failed: Invalid or missing UserID in session");
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                var employee = await _context.Users
                .Where(u => u.UserID == employeeId && u.UserType == 1 && u.Status == 0)
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync();
                if (employee == null)
                {
                    _logger.LogWarning("UpdateReservation failed: Employee with UserID {UserID} not found or inactive", employeeId);
                    return Json(new { success = false, message = "Nhân viên không hợp lệ hoặc không hoạt động." });
                }

                var hasPermission = employee.Role?.RolePermissions
                .Any(rp => rp.PermissionID == 4 && (rp.CanEdit || rp.CanAdd)) == true;
                if (!hasPermission)
                {
                    _logger.LogWarning("UpdateReservation failed: Employee with UserID {UserID} lacks TABLE_BOOKING_MANAGE permission", employeeId);
                    return Json(new { success = false, message = "Bạn không có quyền cập nhật đơn đặt bàn." });
                }

                var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == model.ReservationID);
                if (reservation == null)
                {
                    _logger.LogWarning("UpdateReservation failed: Reservation with ID {ReservationID} not found", model.ReservationID);
                    return Json(new { success = false, message = "Không tìm thấy đơn đặt bàn" });
                }

                if (!Enum.IsDefined(typeof(ReservationStatus), model.Status))
                {
                    return Json(new { success = false, message = "Trạng thái đơn đặt bàn không hợp lệ" });
                }

                if (model.Capacity <= 0)
                {
                    return Json(new { success = false, message = "Số lượng khách phải lớn hơn 0" });
                }

                if (model.Duration <= 0)
                {
                    return Json(new { success = false, message = "Thời gian đặt bàn phải lớn hơn 0" });
                }

                if (string.IsNullOrWhiteSpace(model.CustomerName))
                {
                    return Json(new { success = false, message = "Tên khách hàng không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(model.CustomerPhone))
                {
                    return Json(new { success = false, message = "Số điện thoại không được để trống" });
                }

                reservation.CustomerName = model.CustomerName;
                reservation.CustomerPhone = model.CustomerPhone;
                reservation.Duration = model.Duration;
                reservation.Capacity = model.Capacity;
                reservation.CreatedTime = model.CreatedTime;
                reservation.ReservationTime = model.ReservationTime;
                reservation.Status = model.Status;
                reservation.Notes = model.Notes;

                _context.Entry(reservation).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation("Update reservation successful in {ElapsedMs}ms: ReservationID={ReservationID}",
                stopwatch.ElapsedMilliseconds, reservation.ReservationID);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in UpdateReservation ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi cập nhật đơn đặt bàn" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            _logger.LogDebug("[API] DeleteReservation: ReservationID={ReservationID}", id);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var userIdString = HttpContext.Session.GetString("StaffID");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int employeeId))
                {
                    _logger.LogWarning("DeleteReservation failed: Invalid or missing UserID in session");
                    return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                }

                var employee = await _context.Users
                .Where(u => u.UserID == employeeId && u.UserType == 1 && u.Status == 0)
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .FirstOrDefaultAsync();
                if (employee == null)
                {
                    _logger.LogWarning("DeleteReservation failed: Employee with UserID {UserID} not found or inactive", employeeId);
                    return Json(new { success = false, message = "Nhân viên không hợp lệ hoặc không hoạt động." });
                }

                var hasPermission = employee.Role?.RolePermissions
                .Any(rp => rp.PermissionID == 4 && rp.CanDelete) == true;
                if (!hasPermission)
                {
                    _logger.LogWarning("DeleteReservation failed: Employee with UserID {UserID} lacks TABLE_BOOKING_MANAGE delete permission", employeeId);
                    return Json(new { success = false, message = "Bạn không có quyền xóa đơn đặt bàn." });
                }

                var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == id);
                if (reservation == null)
                {
                    _logger.LogWarning("DeleteReservation failed: Reservation with ID {ReservationID} not found", id);
                    return Json(new { success = false, message = "Không tìm thấy đơn đặt bàn" });
                }

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation("Reservation deleted successfully in {ElapsedMs}ms: ReservationID={ReservationID}",
                stopwatch.ElapsedMilliseconds, id);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in DeleteReservation ({ElapsedMs}ms): {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
                return Json(new { success = false, message = "Lỗi khi xóa đơn đặt bàn" });
            }
        }
    }

    public enum ReservationStatus : byte
    {
        Canceled = 0,
        Confirmed = 1,
        Completed = 2
    }

    public class ReservationUpdateModel
    {
        public int ReservationID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int Duration { get; set; }
        public int Capacity { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ReservationTime { get; set; }
        public byte Status { get; set; }
        public string Notes { get; set; }
    }
}