using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyKitchen.Models;

namespace HappyKitchen.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<RevenueData> GetRevenueDataAsync(string timeRange);
        Task<List<TopSellingFood>> GetTopSellingFoodsAsync(string timeRange);
    }

    public class DashboardStats
    {
        public decimal MonthlyRevenue { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public int MonthlyOrders { get; set; }
        public decimal OrdersChangePercent { get; set; }
        public int NewCustomers { get; set; }
        public decimal NewCustomersChangePercent { get; set; }
        public int TableUsagePercent { get; set; }
        public decimal TableUsageChangePercent { get; set; }
    }

    public class RevenueData
    {
        public List<string> Labels { get; set; }
        public List<decimal> Data { get; set; }
    }

    public class TopSellingFood
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public int SoldQuantity { get; set; }
    }
}