
using Microsoft.EntityFrameworkCore;
using HappyKitchen.Data;
using HappyKitchen.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyKitchen.Services
{
    public interface IMenuItemService
    {
        Task<List<MenuItem>> GetAllMenuItemsAsync();
        Task<MenuItem> GetMenuItemByIdAsync(int id);
        Task CreateMenuItemAsync(MenuItem menuItem);
        Task UpdateMenuItemAsync(MenuItem menuItem);
        Task DeleteMenuItemAsync(int id);
    }
    public class MenuItemService : IMenuItemService
    {
        private readonly ApplicationDbContext _context;

        public MenuItemService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Attributes)
                .Include(m => m.Ratings)
                .ToListAsync();
        }

        public async Task<MenuItem> GetMenuItemByIdAsync(int id)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Attributes)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.MenuItemID == id);
        }

        public async Task CreateMenuItemAsync(MenuItem menuItem)
        {
            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMenuItemAsync(MenuItem menuItem)
        {
            _context.Entry(menuItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMenuItemAsync(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
            }
        }
    }
}