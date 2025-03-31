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
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, IConfiguration configuration, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var tables = _context.Tables.ToList();
            var cartJson = HttpContext.Session.GetString("CartSession");

            var cartItems = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            var viewModel = new HomeIndexViewModel
            {
                Tables = tables,
                CartItems = cartItems
            };

            return View(viewModel);
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
        public async Task<JsonResult> GetAvailableTables(DateTime startTime, int people, int duration)
        {
            _logger.LogInformation($"Checking availability for: {startTime} - {startTime.AddHours(duration)}, People: {people}");

            var reservedTableIds = await _context.Reservations
                .Where(r => r.ReservationTime.Date == startTime.Date)
                .Where(r => startTime < r.ReservationTime.AddMinutes(r.Duration) && startTime.AddHours(duration) > r.ReservationTime)
                .Select(r => r.TableID)
                .ToListAsync();

            _logger.LogInformation($"Reserved Table IDs: {string.Join(", ", reservedTableIds)}");

            var availableTables = await _context.Tables
                .Where(t => t.Capacity >= people && !reservedTableIds.Contains(t.TableID))
                .Select(t => new { t.TableID, t.TableName, t.Capacity })
                .ToListAsync();

            _logger.LogInformation($"Available Tables: {string.Join(", ", availableTables.Select(t => t.TableID))}");

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
