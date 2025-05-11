using Microsoft.AspNetCore.Mvc;
using HappyKitchen.Services;
using HappyKitchen.Attributes;

namespace HappyKitchen.Controllers
{
    [AuthorizeAccess]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IDashboardService _dashboardService;

        public DashboardController(ILogger<DashboardController> logger, IDashboardService dashboardService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
        }

        [AuthorizeAccess("VIEW_REPORT", "view")]
        public async Task<IActionResult> Index()
        {
            return View(); 
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê dashboard");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy dữ liệu thống kê");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(string timeRange = "month", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var revenueData = await _dashboardService.GetRevenueDataAsync(timeRange, startDate, endDate);
                return Json(revenueData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu doanh thu");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy dữ liệu doanh thu");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTopSellingFoods(string timeRange = "day", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var topFoods = await _dashboardService.GetTopSellingFoodsAsync(timeRange, startDate, endDate);
                return Json(topFoods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu món ăn bán chạy");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy dữ liệu món ăn bán chạy");
            }
        }

        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}