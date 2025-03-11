using Microsoft.AspNetCore.Mvc;

namespace HappyKitchen.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult BillManagement()
        {
            return View();
        }
    }
}
