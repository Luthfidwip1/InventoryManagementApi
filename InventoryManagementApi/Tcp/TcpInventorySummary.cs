namespace InventoryManagementApi.Tcp;

public class TcpInventorySummary
{
    public int TotalProducts { get; set; }

    public int TotalUnits { get; set; }

    public int LowStockCount { get; set; }

    public decimal StockValue { get; set; }
}