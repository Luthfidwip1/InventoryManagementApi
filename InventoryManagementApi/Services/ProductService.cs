using InventoryManagementApi.Data;
using InventoryManagementApi.DTOs.Products;
using InventoryManagementApi.Data.Entities;
using InventoryManagementApi.Tcp;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementApi.Services;

public class ProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductResponse>> GetAllAsync(
        string? search = null,
        int? categoryId = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();

            query = query.Where(p =>
                p.Name.ToLower().Contains(keyword) ||
                p.Sku.ToLower().Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p =>
                p.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                QuantityInStock = p.QuantityInStock,
                ReorderLevel = p.ReorderLevel,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                QuantityInStock = p.QuantityInStock,
                ReorderLevel = p.ReorderLevel,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId);

        if (!categoryExists)
        {
            throw new InvalidOperationException(
                "Category not found.");
        }

        var sku = request.Sku.Trim();

        var skuExists = await _context.Products
            .AnyAsync(p =>
                p.Sku.ToLower() == sku.ToLower());

        if (skuExists)
        {
            throw new InvalidOperationException(
                "SKU already exists.");
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Sku = sku,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            UnitPrice = request.UnitPrice,
            QuantityInStock = 0,
            ReorderLevel = request.ReorderLevel,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(product.Id)
            ?? throw new InvalidOperationException(
                "Failed to create product.");
    }

    public async Task<ProductResponse?> UpdateAsync(
        int id,
        UpdateProductRequest request)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return null;
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId);

        if (!categoryExists)
        {
            throw new InvalidOperationException(
                "Category not found.");
        }

        var sku = request.Sku.Trim();

        var duplicateSku = await _context.Products
            .AnyAsync(p =>
                p.Id != id &&
                p.Sku.ToLower() == sku.ToLower());

        if (duplicateSku)
        {
            throw new InvalidOperationException(
                "SKU already exists.");
        }

        product.CategoryId = request.CategoryId;
        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.UnitPrice = request.UnitPrice;
        product.ReorderLevel = request.ReorderLevel;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<List<ProductResponse>> GetLowStockAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p =>
                p.QuantityInStock <= p.ReorderLevel)
            .OrderBy(p => p.QuantityInStock)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                QuantityInStock = p.QuantityInStock,
                ReorderLevel = p.ReorderLevel,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<TcpInventorySummary> GetSummaryAsync()
    {
        var totalProducts =
            await _context.Products.CountAsync();

        var totalUnits =
            await _context.Products
                .SumAsync(p => p.QuantityInStock);

        var lowStockCount =
            await _context.Products
                .CountAsync(p =>
                    p.QuantityInStock <= p.ReorderLevel);

        var stockValue =
            await _context.Products
                .SumAsync(p =>
                    p.UnitPrice * p.QuantityInStock);

        return new TcpInventorySummary
        {
            TotalProducts = totalProducts,
            TotalUnits = totalUnits,
            LowStockCount = lowStockCount,
            StockValue = stockValue
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return false;
        }

        var hasTransactions = await _context.StockTransactions
            .AnyAsync(st => st.ProductId == id);

        if (hasTransactions)
        {
            throw new InvalidOperationException(
                "Product cannot be deleted because it has stock transactions.");
        }

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        return true;
    }
}