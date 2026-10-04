namespace InventoryManagementApi.Data.Entities;

public class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public decimal UnitPrice { get; set; }

    public int QuantityInStock { get; set; } = 0;

    public int ReorderLevel { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<StockTransaction> StockTransactions { get; set; }
        = new List<StockTransaction>();
}