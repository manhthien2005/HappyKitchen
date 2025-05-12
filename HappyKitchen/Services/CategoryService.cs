using HappyKitchen.Data;
using HappyKitchen.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyKitchen.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.MenuItems)
                .ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.CategoryID == id);
        }
        public async Task<Category> GetCategoryByNameAsync(string name)
        {
            return await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.CategoryName == name);
        }

        public async Task CreateCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }
    }
}