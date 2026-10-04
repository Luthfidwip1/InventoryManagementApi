using InventoryManagementApi.Data;
using InventoryManagementApi.DTOs.Categories;
using InventoryManagementApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementApi.Services;

public class CategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryResponse>> GetAllAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                ProductCount = _context.Products.Count(p => p.CategoryId == c.Id)
            })
            .ToListAsync();
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        return await _context.Categories
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                ProductCount = _context.Products.Count(p => p.CategoryId == c.Id)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        var name = request.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new InvalidOperationException("Category name already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description?.Trim()
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            ProductCount = 0
        };
    }

    public async Task<CategoryResponse?> UpdateAsync(
        int id,
        UpdateCategoryRequest request)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return null;
        }

        var name = request.Name.Trim();

        var duplicate = await _context.Categories
            .AnyAsync(c =>
                c.Id != id &&
                c.Name.ToLower() == name.ToLower());

        if (duplicate)
        {
            throw new InvalidOperationException("Category name already exists.");
        }

        category.Name = name;
        category.Description = request.Description?.Trim();

        await _context.SaveChangesAsync();

        var productCount = await _context.Products
            .CountAsync(p => p.CategoryId == category.Id);

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            ProductCount = productCount
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return false;
        }

        var hasProducts = await _context.Products
            .AnyAsync(p => p.CategoryId == id);

        if (hasProducts)
        {
            throw new InvalidOperationException(
                "Category cannot be deleted because it is used by one or more products."
            );
        }

        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        return true;
    }
}