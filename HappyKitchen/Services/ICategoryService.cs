using HappyKitchen.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HappyKitchen.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category> GetCategoryByIdAsync(int id);
        Task<Category> GetCategoryByNameAsync(string name);
        Task CreateCategoryAsync(Category category);
    }
}