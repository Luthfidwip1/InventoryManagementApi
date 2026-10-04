namespace InventoryManagementApi.Tcp;

public class TcpResponse
{
    public string RequestId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public object? Data { get; set; }

    public TcpError? Error { get; set; }

    public static TcpResponse Ok(
        string requestId,
        object? data)
    {
        return new TcpResponse
        {
            RequestId = requestId,
            Status = "ok",
            Data = data,
            Error = null
        };
    }

    public static TcpResponse Fail(
        string requestId,
        string code,
        string message)
    {
        return new TcpResponse
        {
            RequestId = requestId,
            Status = "error",
            Data = null,
            Error = new TcpError
            {
                Code = code,
                Message = message
            }
        };
    }
}

public class TcpError
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}