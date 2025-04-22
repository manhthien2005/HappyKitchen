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
        public async Task<IActionResult> Menuing(string name, string phone, int table, int person, DateTime reservationDate, int duration, string message)
        {
            var selectTable = await _context.Tables
                .Where(c => c.TableID == table)
                .FirstOrDefaultAsync();
            var reservation = new Reservation
            {
                CustomerName = name,
                CustomerPhone = phone,
                TableID = table,
                Capacity = person,
                ReservationTime = reservationDate,
                Duration = duration,
                Notes = message,
                Status = 1,
                Table = selectTable

            };

            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .Where(c => c.MenuItems.Any(m => m.Status == 1))
                .ToListAsync();

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            reservation.Table = selectTable;

            var menuView = new MenuViewModel
            {
                ReservationInformation = reservation,
                Categories = categories
            };



            return View(menuView);
        }

        public IActionResult DishChecking()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DishChecking( string SelectedItemsJson)
        {



            var selectedList = JsonConvert.DeserializeObject<List<CartItem>>(SelectedItemsJson);

            foreach (var item in selectedList)
            {
                item.MenuItem =  _context.MenuItems.FirstOrDefault(m => m.MenuItemID == item.MenuItemID);
            }

            var dishPayingView = new Cart
            {
                Items = selectedList,
            };

            HttpContext.Session.SetString("CartSession", JsonConvert.SerializeObject(selectedList));

            return View(dishPayingView);
        }
    }
}
