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
            return View();
        }

        public async Task<IActionResult> Menu()
        {
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.Status == 1)) 
                .ToListAsync();

            return View(categories);
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
