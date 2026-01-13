using NormalMi.API.Models;

namespace NormalMi.API.Services;

public interface ICategoryService
{
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(string id);
}

