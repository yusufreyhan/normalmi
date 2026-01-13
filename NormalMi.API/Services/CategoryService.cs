using NormalMi.API.Models;

namespace NormalMi.API.Services;

public class CategoryService : ICategoryService
{
    private readonly List<Category> _categories = new()
    {
        new Category
        {
            Id = "meyve-sebze",
            Name = "Meyve & Sebze",
            Route = "/meyve-sebze",
            Description = "Meyve ve sebze fiyatlarını karşılaştırın",
            ImageUrl = "https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400&h=300&fit=crop",
            IsActive = true
        },
        new Category
        {
            Id = "oyunlar",
            Name = "Bilgisayar Oyunu",
            Route = "/oyunlar",
            Description = "Bilgisayar oyunu fiyatlarını karşılaştırın",
            ImageUrl = "https://images.unsplash.com/photo-1552820728-8b83bb6b773f?w=400&h=300&fit=crop",
            IsActive = true
        },
        new Category
        {
            Id = "akaryakit",
            Name = "Akaryakıt",
            Route = "/akaryakit",
            Description = "Akaryakıt fiyatlarını karşılaştırın",
            ImageUrl = "https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=400&h=300&fit=crop",
            IsActive = false // MVP'de aktif değil
        },
        new Category
        {
            Id = "kira",
            Name = "Kira",
            Route = "/kira",
            Description = "Kira fiyatlarını karşılaştırın",
            ImageUrl = "https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=400&h=300&fit=crop",
            IsActive = false // MVP'de aktif değil
        },
        new Category
        {
            Id = "elektrik",
            Name = "Elektrik",
            Route = "/elektrik",
            Description = "Elektrik fiyatlarını karşılaştırın",
            ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=300&fit=crop",
            IsActive = false // MVP'de aktif değil
        },
        new Category
        {
            Id = "telefon-tarifeleri",
            Name = "Telefon Tarifeleri",
            Route = "/telefon-tarifeleri",
            Description = "Telefon tarifesi fiyatlarını karşılaştırın",
            ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400&h=300&fit=crop",
            IsActive = false // MVP'de aktif değil
        }
    };

    public Task<List<Category>> GetAllCategoriesAsync()
    {
        return Task.FromResult(_categories);
    }

    public Task<Category?> GetCategoryByIdAsync(string id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(category);
    }
}

