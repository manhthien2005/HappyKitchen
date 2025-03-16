using Microsoft.AspNetCore.Mvc;

namespace HappyKitchen.Controllers
{
    public class ReservationController : Controller
    {
        public IActionResult Menuing()
        {
            return View();
        }

        public IActionResult DishChecking()
        {
            return View();
        }
    }
}
