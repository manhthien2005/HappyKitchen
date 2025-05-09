using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
