using InventoryManagementApi.Data;
using InventoryManagementApi.DTOs.StockTransactions;
using InventoryManagementApi.Enums;
using InventoryManagementApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementApi.Services;

public class StockService
{
    private readonly ApplicationDbContext _context;

    public StockService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockTransactionResponse> CreateAsync(
        CreateStockTransactionRequest request,
        StockTransactionSource source = StockTransactionSource.Api)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product == null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        if (request.Quantity == 0)
        {
            throw new InvalidOperationException("Quantity cannot be zero.");
        }

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            switch (request.Type)
            {
                case StockTransactionType.In:
                    if (request.Quantity <= 0)
                    {
                        throw new InvalidOperationException(
                            "In transaction quantity must be positive.");
                    }

                    product.QuantityInStock += request.Quantity;
                    break;

                case StockTransactionType.Out:
                    if (request.Quantity <= 0)
                    {
                        throw new InvalidOperationException(
                            "Out transaction quantity must be positive.");
                    }

                    if (product.QuantityInStock < request.Quantity)
                    {
                        throw new InvalidOperationException(
                            "Insufficient stock.");
                    }

                    product.QuantityInStock -= request.Quantity;
                    break;

                case StockTransactionType.Adjustment:
                    var newStock =
                        product.QuantityInStock + request.Quantity;

                    if (newStock < 0)
                    {
                        throw new InvalidOperationException(
                            "Adjustment would make stock negative.");
                    }

                    product.QuantityInStock = newStock;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Invalid transaction type.");
            }

            product.UpdatedAt = DateTime.UtcNow;

            var transaction = new StockTransaction
            {
                ProductId = request.ProductId,
                Type = request.Type,
                Quantity = request.Quantity,
                Note = request.Note?.Trim(),
                TransactionDate = DateTime.UtcNow,
                Source = source
            };

            _context.StockTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return new StockTransactionResponse
            {
                Id = transaction.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Type = transaction.Type.ToString(),
                Quantity = transaction.Quantity,
                Note = transaction.Note,
                TransactionDate = transaction.TransactionDate,
                Source = transaction.Source.ToString(),
                QuantityInStock = product.QuantityInStock
            };
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<StockTransactionResponse>> GetAllAsync(
        int? productId = null,
        StockTransactionType? type = null,
        DateTime? from = null,
        DateTime? to = null,
        StockTransactionSource? source = null)
    {
        var query = _context.StockTransactions
            .Include(st => st.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(st =>
                st.ProductId == productId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(st =>
                st.Type == type.Value);
        }

        if (from.HasValue)
        {
            var fromUtc = NormalizeToUtc(from.Value);

            query = query.Where(st =>
                st.TransactionDate >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = NormalizeToUtc(to.Value);

            if (toUtc.TimeOfDay == TimeSpan.Zero)
            {
                var nextDay = toUtc.Date.AddDays(1);

                query = query.Where(st =>
                    st.TransactionDate < nextDay);
            }
            else
            {
                query = query.Where(st =>
                    st.TransactionDate <= toUtc);
            }
        }

        if (source.HasValue)
        {
            query = query.Where(st =>
                st.Source == source.Value);
        }

        return await query
            .OrderByDescending(st => st.TransactionDate)
            .Select(st => new StockTransactionResponse
            {
                Id = st.Id,
                ProductId = st.ProductId,
                ProductName = st.Product.Name,
                Type = st.Type.ToString(),
                Quantity = st.Quantity,
                Note = st.Note,
                TransactionDate = st.TransactionDate,
                Source = st.Source.ToString(),
                QuantityInStock = st.Product.QuantityInStock
            })
            .ToListAsync();
    }

    public async Task<StockTransactionResponse?> GetByIdAsync(int id)
    {
        return await _context.StockTransactions
            .Include(st => st.Product)
            .Where(st => st.Id == id)
            .Select(st => new StockTransactionResponse
            {
                Id = st.Id,
                ProductId = st.ProductId,
                ProductName = st.Product.Name,
                Type = st.Type.ToString(),
                Quantity = st.Quantity,
                Note = st.Note,
                TransactionDate = st.TransactionDate,
                Source = st.Source.ToString(),
                QuantityInStock = st.Product.QuantityInStock
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<StockTransactionResponse>> GetByProductIdAsync(
        int productId)
    {
        return await _context.StockTransactions
            .Include(st => st.Product)
            .Where(st => st.ProductId == productId)
            .OrderByDescending(st => st.TransactionDate)
            .Select(st => new StockTransactionResponse
            {
                Id = st.Id,
                ProductId = st.ProductId,
                ProductName = st.Product.Name,
                Type = st.Type.ToString(),
                Quantity = st.Quantity,
                Note = st.Note,
                TransactionDate = st.TransactionDate,
                Source = st.Source.ToString(),
                QuantityInStock = st.Product.QuantityInStock
            })
            .ToListAsync();
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}