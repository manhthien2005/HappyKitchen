using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyKitchen.Data;
using HappyKitchen.Models;
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

        public async Task<RevenueData> GetRevenueDataAsync(string timeRange)
        {
            var currentDate = DateTime.Now;
            var labels = new List<string>();
            var data = new List<decimal>();

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

                        data.Add(monthlyRevenue); // Chuyển đổi sang đơn vị nghìn
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

                        data.Add(monthlyRevenue); // Chuyển đổi sang đơn vị nghìn
                    }
                    break;
            }

            return new RevenueData
            {
                Labels = labels,
                Data = data
            };
        }

        public async Task<List<TopSellingFood>> GetTopSellingFoodsAsync(string timeRange)
        {
            var currentDate = DateTime.Now;
            DateTime startDate;

            switch (timeRange)
            {
                case "day":
                    startDate = currentDate.Date;
                    break;
                case "week":
                    startDate = currentDate.AddDays(-(int)currentDate.DayOfWeek).Date;
                    break;
                case "month":
                    startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                    break;
                default:
                    startDate = currentDate.Date;
                    break;
            }

            // Lấy top 5 món ăn bán chạy nhất trong khoảng thời gian
            var topFoods = await _context.OrderDetails
                .Where(oi => oi.Order.OrderTime >= startDate && oi.Order.Status >= 3)
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