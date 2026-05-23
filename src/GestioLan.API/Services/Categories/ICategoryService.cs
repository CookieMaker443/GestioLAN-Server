using GestioLan.API.Models;

namespace GestioLan.API.Services.Categories;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category> AddCategoryAsync(string nome);
    Task<string> UpdateCategoryAsync(int id, Category category);
    Task<Category> DeleteCategoryAsync(int id);
}