using HappyKitchen.Data;
using HappyKitchen.Models;
using HappyKitchen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;
using System.Net.WebSockets;
using static System.Net.WebRequestMethods;

namespace HappyKitchen.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var tables = _context.Tables.ToList();
            return View(tables);
        }

        public async Task<IActionResult> Menu()
        {
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.Status == 1)) 
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableTables( DateTime startTime, int people, int duration)
        {

            var startDate = startTime.Date; // Chỉ lọc trong cùng ngày
            var endTime = startTime.AddHours(duration);

            var reservedTableIds = await _context.Reservations
                .Where(r => r.ReservationTime.Date == startDate) // Chỉ lấy đặt chỗ trong cùng ngày
                .Where(r => startTime < r.ReservationTime.AddMinutes(r.Duration) && endTime > r.ReservationTime)
                .Select(r => r.TableID)
                .ToListAsync();

            var availableTables = await _context.Tables
                .Where(t => t.Capacity >= people && !reservedTableIds.Contains(t.TableID))
                .Select(t => new { t.TableID, t.TableName, t.Capacity})
                .ToListAsync();

            return Json(availableTables);
        }

        public async Task<IActionResult> DetailDish(int MenuItemID)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Attributes)
                .Include(m => m.Ratings)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.MenuItemID == MenuItemID); 

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem); 
        }

        public async Task<IActionResult> Event()
        {
            return View();
        }

        
    }
}
