namespace InventoryManagementApi.DTOs.StockTransactions;

public class StockTransactionResponse
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? Note { get; set; }

    public DateTime TransactionDate { get; set; }

    public string Source { get; set; } = string.Empty;

    public int QuantityInStock { get; set; }
}