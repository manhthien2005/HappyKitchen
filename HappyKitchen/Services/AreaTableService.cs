using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyKitchen.Services
{
    public class AreaTableService : IAreaTableService
    {
        private readonly ApplicationDbContext _context;

        public AreaTableService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Area>> GetAllAreasAsync(string searchTerm = "")
        {
            var query = _context.Areas
            
                .Include(a => a.Tables)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(a => a.AreaName.ToLower().Contains(searchTerm) ||
                                        (a.Description != null && a.Description.ToLower().Contains(searchTerm)));
            }

            return await query.OrderBy(a => a.AreaName)
                    .ToListAsync();
        }

        public async Task<Area> GetAreaByIdAsync(int areaId)
        {
            return await _context.Areas.FindAsync(areaId) ?? throw new KeyNotFoundException("Area not found");
        }

        public async Task CreateAreaAsync(Area area)
        {
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAreaAsync(Area area)
        {
            _context.Areas.Update(area);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAreaAsync(int areaId)
        {
            var area = await GetAreaByIdAsync(areaId);
            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();
        }

        // Table Operations
        public async Task<List<Table>> GetAllTablesAsync(string searchTerm = "", int areaId = 0, string status = "all")
        {
            var query = _context.Tables
                .Include(t => t.Area)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(t => t.TableName.ToLower().Contains(searchTerm));
            }

            if (areaId > 0)
            {
                query = query.Where(t => t.AreaID == areaId);
            }

            if (status != "all")
            {
                byte statusValue = byte.Parse(status);
                query = query.Where(t => t.Status == statusValue);
            }

            return await query.OrderBy(t => t.TableName).ToListAsync();
        }

        public async Task<Table> GetTableByIdAsync(int tableId)
        {
            return await _context.Tables
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.TableID == tableId) ?? throw new KeyNotFoundException("Table not found");
        }

        public async Task CreateTableAsync(Table table)
        {
            _context.Tables.Add(table);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTableAsync(Table table)
        {
            _context.Tables.Update(table);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTableAsync(int tableId)
        {
            var table = await GetTableByIdAsync(tableId);
            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
        }
    }
}