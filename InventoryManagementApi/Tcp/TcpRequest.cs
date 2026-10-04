using System.Text.Json;

namespace InventoryManagementApi.Tcp;

public class TcpRequest
{
    public string RequestId { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public JsonElement? Data { get; set; }
}