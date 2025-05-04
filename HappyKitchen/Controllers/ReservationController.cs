using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HappyKitchen.Controllers
{
    public class ReservationController : Controller
    {

        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Menuing()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Menuing([FromBody] DishCheckingViewModel cartInfoJson)
        {
            // Kiểm tra dish hoặc ReservationInformation có null không
            if (cartInfoJson == null )
            {
                
                return BadRequest("Dữ liệu đặt bàn không hợp lệ.");
            }

            Console.WriteLine("alooooooo", cartInfoJson.ReservationInformation);

            // Lấy danh sách danh mục món ăn
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.Status == 1))
                .ToListAsync();

            // Kiểm tra bàn được chọn
            var selectTable = await _context.Tables
                .Where(c => c.TableID == cartInfoJson.ReservationInformation.TableID)
                .FirstOrDefaultAsync();

            if (selectTable == null)
            {
                return BadRequest("Bàn được chọn không tồn tại.");
            }
            cartInfoJson.ReservationInformation.Table = selectTable;
            // Kiểm tra xem đặt bàn đã tồn tại chưa
            var existingReservation = await _context.Reservations
                .FirstOrDefaultAsync(r =>
                    r.CustomerPhone == cartInfoJson.ReservationInformation.CustomerPhone &&
                    r.ReservationTime == cartInfoJson.ReservationInformation.ReservationTime &&
                    r.TableID == cartInfoJson.ReservationInformation.TableID);



            // Nếu chưa tồn tại, thêm mới vào database
            if (existingReservation == null)
            {
                _context.Reservations.Add(cartInfoJson.ReservationInformation);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Có thể ghi log hoặc xử lý thêm nếu cần
                Console.WriteLine("Đặt bàn đã tồn tại, không thêm mới.");
            }

            // Tạo view model để trả về
            var menuView = new MenuViewModel
            {
                Cart = cartInfoJson,
                Categories = categories
            };

            return View(menuView);
        }

        public IActionResult DishChecking()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DishChecking([FromBody] DishCheckingViewModel viewModel)
        {
            if (viewModel == null || viewModel.CartItems == null)
            {
                Console.WriteLine("ViewModel is null or invalid");
                return BadRequest("Dữ liệu JSON không hợp lệ.");
            }

            Console.WriteLine($"Deserialized list count: {viewModel.CartItems?.Count ?? 0}");

            foreach (var item in viewModel.CartItems)
            {
                item.MenuItem = _context.MenuItems.FirstOrDefault(m => m.MenuItemID == item.MenuItemID);
                Console.WriteLine($"Item processed - MenuItemID: {item.MenuItemID}, MenuItem: {item.MenuItem?.ToString() ?? "null"}");
            }

            string jsonString = JsonConvert.SerializeObject(viewModel);
            HttpContext.Session.SetString("CartSession", jsonString);

            Console.WriteLine($"Data saved to session: {jsonString}");
            string sessionData = HttpContext.Session.GetString("CartSession");
            Console.WriteLine($"Data retrieved from session: {sessionData}");
            Console.WriteLine($"Session save successful: {string.Equals(jsonString, sessionData)}");

            return View(viewModel);
        }
    }
    
}
