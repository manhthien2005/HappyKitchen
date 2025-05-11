
using HappyKitchen.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyKitchen.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            // Lấy tháng hiện tại và tháng trước
            var currentDate = DateTime.Now;
            var firstDayCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var firstDayPreviousMonth = firstDayCurrentMonth.AddMonths(-1);

            // Tính toán doanh thu tháng hiện tại và tháng trước
            var currentMonthRevenue = await _context.Orders
                .Where(o => o.OrderTime >= firstDayCurrentMonth && o.Status >= 3)
                .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                .SumAsync();

            var previousMonthRevenue = await _context.Orders
                .Where(o => o.OrderTime >= firstDayPreviousMonth && o.OrderTime < firstDayCurrentMonth && o.Status >= 3)
                .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                .SumAsync();

            // Tính toán số đơn hàng tháng hiện tại và tháng trước
            var currentMonthOrders = await _context.Orders
                .Where(o => o.OrderTime >= firstDayCurrentMonth)
                .CountAsync();

            var previousMonthOrders = await _context.Orders
                .Where(o => o.OrderTime >= firstDayPreviousMonth && o.OrderTime < firstDayCurrentMonth)
                .CountAsync();

            // Tính toán số khách hàng mới tháng hiện tại và tháng trước
            var currentMonthNewCustomers = await _context.Users
                .Where(u => u.UserType == 0 && u.CreatedAt >= firstDayCurrentMonth)
                .CountAsync();

            var previousMonthNewCustomers = await _context.Users
                .Where(u => u.UserType == 0 && u.CreatedAt >= firstDayPreviousMonth && u.CreatedAt < firstDayCurrentMonth)
                .CountAsync();

            // Tính toán tỷ lệ sử dụng bàn
            var totalTables = await _context.Tables.CountAsync();
            var usedTables = await _context.Tables.Where(t => t.Status > 0).CountAsync();
            var tableUsagePercent = totalTables > 0 ? (int)Math.Round((double)usedTables / totalTables * 100) : 0;

            // Giả định tỷ lệ sử dụng bàn tháng trước
            var previousMonthTableUsagePercent = tableUsagePercent + 2; // Giả định tháng trước cao hơn 2%

            // Tính phần trăm thay đổi
            var revenueChangePercent = previousMonthRevenue > 0 
                ? Math.Round((decimal)(currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue * 100, 1) 
                : 0;

            var ordersChangePercent = previousMonthOrders > 0
                ? Math.Round((decimal)(currentMonthOrders - previousMonthOrders) / previousMonthOrders * 100, 1) 
                : 0;

            var newCustomersChangePercent = previousMonthNewCustomers > 0
                ? Math.Round((decimal)(currentMonthNewCustomers - previousMonthNewCustomers) / previousMonthNewCustomers * 100, 1) 
                : 0;

            var tableUsageChangePercent = previousMonthTableUsagePercent > 0 
                ? Math.Round((decimal)(tableUsagePercent - previousMonthTableUsagePercent) / previousMonthTableUsagePercent * 100, 1) 
                : 0;

            return new DashboardStats
            {
                MonthlyRevenue = currentMonthRevenue, 
                RevenueChangePercent = revenueChangePercent,
                MonthlyOrders = currentMonthOrders,
                OrdersChangePercent = ordersChangePercent,
                NewCustomers = currentMonthNewCustomers,
                NewCustomersChangePercent = newCustomersChangePercent,
                TableUsagePercent = tableUsagePercent,
                TableUsageChangePercent = tableUsageChangePercent
            };
        }

        public async Task<RevenueData> GetRevenueDataAsync(string timeRange, DateTime? startDate = null, DateTime? endDate = null)
        {
            var currentDate = DateTime.Now;
            var labels = new List<string>();
            var data = new List<decimal>();

            // Xử lý khoảng thời gian tùy chỉnh
            if (timeRange == "custom" && startDate.HasValue && endDate.HasValue)
            {
                // Đảm bảo ngày bắt đầu không lớn hơn ngày kết thúc
                if (startDate.Value > endDate.Value)
                {
                    var temp = startDate;
                    startDate = endDate;
                    endDate = temp;
                }

                // Tính số ngày giữa startDate và endDate
                var daysDifference = (endDate.Value - startDate.Value).Days;

                // Nếu khoảng thời gian lớn hơn 30 ngày, hiển thị theo tháng
                if (daysDifference > 30)
                {
                    // Lấy tháng đầu tiên
                    var currentMonth = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
                    
                    // Lặp qua từng tháng cho đến tháng cuối cùng
                    while (currentMonth <= endDate.Value)
                    {
                        var nextMonth = currentMonth.AddMonths(1);
                        var lastDayOfMonth = nextMonth.AddDays(-1);
                        
                        // Nếu lastDayOfMonth vượt quá endDate, sử dụng endDate
                        var endOfPeriod = lastDayOfMonth > endDate.Value ? endDate.Value : lastDayOfMonth;
                        
                        labels.Add($"T{currentMonth.Month}/{currentMonth.Year}");

                        var monthlyRevenue = await _context.Orders
                            .Where(o => o.OrderTime >= currentMonth && o.OrderTime <= endOfPeriod && o.Status >= 3)
                            .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                            .SumAsync();

                        data.Add(monthlyRevenue);
                        
                        currentMonth = nextMonth;
                    }
                }
                // Nếu khoảng thời gian từ 7-30 ngày, hiển thị theo tuần
                else if (daysDifference > 7)
                {
                    // Chia khoảng thời gian thành các đoạn 7 ngày
                    var currentDay = startDate.Value.Date;
                    
                    while (currentDay <= endDate.Value)
                    {
                        var endOfWeek = currentDay.AddDays(6);
                        
                        // Nếu endOfWeek vượt quá endDate, sử dụng endDate
                        var endOfPeriod = endOfWeek > endDate.Value ? endDate.Value : endOfWeek;
                        
                        labels.Add($"{currentDay.ToString("dd/MM")} - {endOfPeriod.ToString("dd/MM")}");

                        var weeklyRevenue = await _context.Orders
                            .Where(o => o.OrderTime.Date >= currentDay && o.OrderTime.Date <= endOfPeriod && o.Status >= 3)
                            .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                            .SumAsync();

                        data.Add(weeklyRevenue);
                        
                        currentDay = endOfWeek.AddDays(1);
                    }
                }
                // Nếu khoảng thời gian nhỏ hơn hoặc bằng 7 ngày, hiển thị theo ngày
                else
                {
                    for (var date = startDate.Value.Date; date <= endDate.Value.Date; date = date.AddDays(1))
                    {
                        labels.Add(date.ToString("dd/MM"));

                        var dailyRevenue = await _context.Orders
                            .Where(o => o.OrderTime.Date == date && o.Status >= 3)
                            .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                            .SumAsync();

                        data.Add(dailyRevenue);
                    }
                }
            }
            else
            {
                // Xử lý các khoảng thời gian cố định
                switch (timeRange)
                {
                    case "month":
                        // Lấy dữ liệu 15 ngày gần nhất
                        for (int i = 14; i >= 0; i--)
                        {
                            var date = currentDate.AddDays(-i);
                            labels.Add(date.ToString("dd/MM"));

                            var dailyRevenue = await _context.Orders
                                .Where(o => o.OrderTime.Date == date.Date && o.Status >= 3)
                                .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                                .SumAsync();

                            data.Add(dailyRevenue); 
                        }
                        break;

                    case "quarter":
                        // Lấy dữ liệu 3 tháng gần nhất
                        for (int i = 2; i >= 0; i--)
                        {
                            var month = currentDate.AddMonths(-i);
                            var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
                            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                            labels.Add($"Tháng {month.Month}");

                            var monthlyRevenue = await _context.Orders
                                .Where(o => o.OrderTime >= firstDayOfMonth && o.OrderTime <= lastDayOfMonth && o.Status >= 3)
                                .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                                .SumAsync();

                            data.Add(monthlyRevenue);
                        }
                        break;

                    case "year":
                        // Lấy dữ liệu 12 tháng gần nhất
                        for (int i = 11; i >= 0; i--)
                        {
                            var month = currentDate.AddMonths(-i);
                            var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
                            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                            labels.Add($"T{month.Month}");

                            var monthlyRevenue = await _context.Orders
                                .Where(o => o.OrderTime >= firstDayOfMonth && o.OrderTime <= lastDayOfMonth && o.Status >= 3)
                                .Select(o => o.OrderDetails.Sum(od => od.MenuItem.Price * od.Quantity))
                                .SumAsync();

                            data.Add(monthlyRevenue);
                        }
                        break;
                }
            }

            return new RevenueData
            {
                Labels = labels,
                Data = data
            };
        }

        public async Task<List<TopSellingFood>> GetTopSellingFoodsAsync(string timeRange, DateTime? startDate = null, DateTime? endDate = null)
        {
            var currentDate = DateTime.Now;
            DateTime queryStartDate;
            DateTime queryEndDate = currentDate;

            // Xử lý khoảng thời gian tùy chỉnh
            if (timeRange == "custom" && startDate.HasValue && endDate.HasValue)
            {
                queryStartDate = startDate.Value.Date;
                queryEndDate = endDate.Value.Date.AddDays(1).AddSeconds(-1); // Kết thúc của ngày
            }
            else
            {
                // Xử lý các khoảng thời gian cố định
                switch (timeRange)
                {
                    case "day":
                        queryStartDate = currentDate.Date;
                        break;
                    case "week":
                        queryStartDate = currentDate.AddDays(-(int)currentDate.DayOfWeek).Date;
                        break;
                    case "month":
                        queryStartDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                        break;
                    default:
                        queryStartDate = currentDate.Date;
                        break;
                }
            }

            // Lấy top 5 món ăn bán chạy nhất trong khoảng thời gian
            var topFoods = await _context.OrderDetails
                .Where(oi => oi.Order.OrderTime >= queryStartDate && oi.Order.OrderTime <= queryEndDate && oi.Order.Status >= 3)
                .GroupBy(oi => new { oi.MenuItem.MenuItemID, oi.MenuItem.Name, oi.MenuItem.MenuItemImage, oi.MenuItem.Price })
                .Select(g => new TopSellingFood
                {
                    Name = g.Key.Name,
                    Image = g.Key.MenuItemImage,
                    Price = g.Key.Price,
                    SoldQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(f => f.SoldQuantity)
                .Take(5)
                .ToListAsync();

            // Gán thứ hạng
            for (int i = 0; i < topFoods.Count; i++)
            {
                topFoods[i].Rank = i + 1;
            }

            return topFoods;
        }
    }
}