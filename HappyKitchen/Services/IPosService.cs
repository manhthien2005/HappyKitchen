using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                table.Status = (byte)(await _context.Orders
                    .AnyAsync(o => o.TableID == table.TableID && (o.Status == 1 || o.Status == 2)) ? 0 : 1);
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
                table.Status = await _context.Orders
                    .AnyAsync(o => o.TableID == table.TableID && (o.Status == 1 || o.Status == 2)) ? (byte)0 : (byte)1;
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
            table.Status = 0; // Mark table as occupied
            await _context.SaveChangesAsync();
        }
    }
}