using HappyKitchen.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyKitchen.Services
{
    public interface IAreaTableService
    {
        Task<List<Area>> GetAllAreasAsync(string searchTerm = "");
        Task<Area> GetAreaByIdAsync(int areaId);
        Task CreateAreaAsync(Area area);
        Task UpdateAreaAsync(Area area);
        Task DeleteAreaAsync(int areaId);

        Task<List<Table>> GetAllTablesAsync(string searchTerm = "", int areaId = 0, string status = "all");
        Task<Table> GetTableByIdAsync(int tableId);
        Task CreateTableAsync(Table table);
        Task UpdateTableAsync(Table table);
        Task DeleteTableAsync(int tableId);
    }
}