
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;
using HappyKitchen.Data;

namespace HappyKitchen.Services
{
    public interface ITableService
    {
        Task<List<Table>> GetAllTablesAsync();
        Task<Table> GetTableByIdAsync(int id);
        Task UpdateTableAsync(Table table);
    }

    public class TableService : ITableService
    {
        private readonly ApplicationDbContext _context;

        public TableService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Table>> GetAllTablesAsync()
        {
            return await _context.Tables.ToListAsync();
        }

        public async Task<Table> GetTableByIdAsync(int id)
        {
            return await _context.Tables.FindAsync(id);
        }
        public async Task UpdateTableAsync(Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table), "Table cannot be null");
            }

            var existingTable = await _context.Tables.FindAsync(table.TableID);
            if (existingTable == null)
            {
                throw new KeyNotFoundException($"Table with ID {table.TableID} not found");
            }

            // Update properties
            existingTable.TableName = table.TableName;
            existingTable.AreaID = table.AreaID;
            existingTable.Capacity = table.Capacity;
            existingTable.Status = table.Status;

            _context.Tables.Update(existingTable);
            await _context.SaveChangesAsync();
        }
    }
}