using InventoryManagementApi.Enums;

namespace InventoryManagementApi.Data.Entities;

public class StockTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public StockTransactionType Type { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public StockTransactionSource Source { get; set; }

    public Product Product { get; set; } = null!;
}