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
            // Lấy session
            string cartJson = HttpContext.Session.GetString("CartSession");
            Console.WriteLine($"DEBUG: Data retrieved from session: {cartJson ?? "null"}");

            // Khởi tạo cartItems mặc định
            DishCheckingViewModel cartItems = new DishCheckingViewModel
            {
                ReservationInformation = null,
                CartItems = new List<CartItem>()
            };

            // Deserialize cartJson nếu tồn tại
            if (!string.IsNullOrEmpty(cartJson))
            {
                try
                {
                    cartItems = JsonConvert.DeserializeObject<DishCheckingViewModel>(cartJson);
                    Console.WriteLine($"DEBUG: Deserialized cartItems: {(cartItems != null ? $"CartItems count: {cartItems.CartItems?.Count ?? 0}" : "null")}");

                    // Kiểm tra và khởi tạo CartItems nếu null
                    if (cartItems != null && cartItems.CartItems == null)
                    {
                        Console.WriteLine("DEBUG: CartItems is null, initializing to empty list");
                        cartItems.CartItems = new List<CartItem>();
                    }

                    // Log chi tiết các CartItem
                    if (cartItems?.CartItems != null)
                    {
                        foreach (var item in cartItems.CartItems)
                        {
                            Console.WriteLine($"DEBUG: CartItem - MenuItemID: {item.MenuItemID}, MenuItem: {item.MenuItem?.ToString() ?? "null"}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Deserialize error: {ex.Message}");
                    // Giữ cartItems mặc định (rỗng) nếu deserialize thất bại
                    cartItems = new DishCheckingViewModel
                    {
                        ReservationInformation = null,
                        CartItems = new List<CartItem>()
                    };
                }
            }
            else
            {
                Console.WriteLine("DEBUG: cartJson is null or empty, using default empty cart");
            }

            // Lấy danh sách bàn
            var tables = _context.Tables.ToList();

            // Tạo view model
            var viewModel = new HomeIndexViewModel
            {
                Tables = tables,
                Cart = cartItems
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

            menuItem.Ratings = menuItem.Ratings.OrderByDescending(r => r.CreatedAt).ToList();

            return View(menuItem); 
        }

        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] CommentRequestModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Cần phải đăng nhập để đăng đánh giá!" });
            }

            if (model.Rating < 1 || model.Rating > 5)
            {
                return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao." });
            }

            var menuItemRating = new MenuItemRating
            {
                MenuItemID = model.MenuItemId,
                UserID = userId.Value,
                Rating = (byte)model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.Now
            };

            _context.MenuItemRatings.Add(menuItemRating);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId.Value);

            var result = new
            {
                success = true,
                author = user?.FullName ?? "Anonymous",
                date = menuItemRating.CreatedAt.ToString("dd/MM/yyyy"),
                rating = menuItemRating.Rating,
                text = menuItemRating.Comment
            };

            return Json(result);
        }

        public async Task<IActionResult> Event()
        {
            return View();
        }

        public async Task<IActionResult> AboutUs()
        {
            return View();
        }


    }
}
