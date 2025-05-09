using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HappyKitchen.Services
{
    public interface IPosService
    {
        Task<List<Table>> GetAllTablesAsync();
        Task<Table> GetTableByIdAsync(int tableId);
        Task<List<User>> GetCustomersAsync(string searchTerm);
        Task CreateOrderAsync(Order order);
    }

    public class PosService : IPosService
    {
        private readonly ApplicationDbContext _context;

        public PosService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Table>> GetAllTablesAsync()
        {
            var tables = await _context.Tables
                .Include(t => t.Area)
                .ToListAsync();

            foreach (var table in tables)
            {
                // Kiểm tra đặt bàn đang hoạt động
                bool hasActiveReservation = await _context.Reservations
                    .AnyAsync(r => r.TableID == table.TableID
                        && r.Status == 1 // Xác nhận
                        && DateTime.Now >= r.ReservationTime
                        && DateTime.Now < r.ReservationTime.AddMinutes(r.Duration));

                // Kiểm tra đơn hàng đang hoạt động
                bool hasActiveOrder = await _context.Orders
                    .AnyAsync(o => o.TableID == table.TableID
                        && o.Status >= 1 && o.Status <= 2 // Chờ xác nhận hoặc đang chuẩn bị
                        && o.OrderTime >= DateTime.Now.AddHours(-4)); // Hoạt động trong 4 giờ gần nhất

                table.Status = hasActiveReservation ? (byte)1 // Đã đặt trước
                    : hasActiveOrder ? (byte)2 // Đang sử dụng
                    : (byte)0; // Trống
            }

            return tables;
        }

        public async Task<Table> GetTableByIdAsync(int tableId)
        {
            var table = await _context.Tables
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.TableID == tableId);

            if (table != null)
            {
                bool hasActiveReservation = await _context.Reservations
                    .AnyAsync(r => r.TableID == table.TableID
                        && r.Status == 1
                        && DateTime.Now >= r.ReservationTime
                        && DateTime.Now < r.ReservationTime.AddMinutes(r.Duration));

                bool hasActiveOrder = await _context.Orders
                    .AnyAsync(o => o.TableID == table.TableID
                        && o.Status >= 1 && o.Status <= 2
                        && o.OrderTime >= DateTime.Now.AddHours(-4));

                table.Status = hasActiveReservation ? (byte)1
                    : hasActiveOrder ? (byte)2
                    : (byte)0;
            }

            return table;
        }

        public async Task<List<User>> GetCustomersAsync(string searchTerm)
        {
            var query = _context.Users
                .Where(u => u.UserType == 0 && u.Status == 0); // Customers only, active
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm));
            }
            return await query.Take(10).ToListAsync();
        }

        public async Task CreateOrderAsync(Order order)
        {
            var table = await _context.Tables.FindAsync(order.TableID);
            if (table == null)
            {
                throw new Exception("Bàn không tồn tại");
            }
            foreach (var detail in order.OrderDetails)
            {
                var menuItem = await _context.MenuItems.FindAsync(detail.MenuItemID);
                if (menuItem == null || menuItem.Status == 0)
                {
                    throw new Exception($"Món {detail.MenuItemID} không khả dụng");
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Cập nhật trạng thái bàn
            await UpdateTableStatusAsync(new List<int> { order.TableID });
        }

        public async Task UpdateTableStatusAsync(List<int> tableIds)
        {
            if (tableIds == null || !tableIds.Any())
                return;

            string tableIdsString = string.Join(",", tableIds);
            await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateTableStatusByIds @TableIds",
                new SqlParameter("@TableIds", tableIdsString));
        }
    }
}