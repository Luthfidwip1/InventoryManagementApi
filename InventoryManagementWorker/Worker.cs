using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace InventoryManagementWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var startupDelaySeconds =
            _configuration.GetValue<int?>(
                "Worker:StartupDelaySeconds")
            ?? 3;

        if (startupDelaySeconds > 0)
        {
            _logger.LogInformation(
                "Worker waiting {Seconds} seconds for Inventory API to start...",
                startupDelaySeconds);

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(startupDelaySeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "===== Inventory Worker Cycle Started at {Time} =====",
                    DateTimeOffset.Now);

                await EnsureConnectedAsync(stoppingToken);

                await RunInventoryCycleAsync(stoppingToken);

                _logger.LogInformation(
                    "===== Inventory Worker Cycle Finished =====");
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "TCP request timed out.");

                Disconnect();
            }
            catch (SocketException ex)
            {
                _logger.LogError(
                    ex,
                    "TCP connection failed.");

                Disconnect();
            }
            catch (IOException ex)
            {
                _logger.LogError(
                    ex,
                    "TCP connection was lost.");

                Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected worker error.");

                Disconnect();
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        GetCycleIntervalSeconds()),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        Disconnect();
    }

    private async Task RunInventoryCycleAsync(
        CancellationToken stoppingToken)
    {
        var pingResponse = await SendRequestAsync(
            new TcpRequest
            {
                RequestId = CreateRequestId(),
                Command = "PING"
            },
            stoppingToken);

        if (!IsSuccess(pingResponse))
        {
            throw new InvalidOperationException(
                $"PING failed: {GetErrorMessage(pingResponse)}");
        }

        var pong =
            pingResponse.Data.ValueKind == JsonValueKind.String
                ? pingResponse.Data.GetString()
                : null;

        if (!string.Equals(
                pong,
                "PONG",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid PING response from TCP server.");
        }

        _logger.LogInformation(
            "TCP server responded with PONG.");

        var lowStockResponse = await SendRequestAsync(
            new TcpRequest
            {
                RequestId = CreateRequestId(),
                Command = "GET_LOW_STOCK"
            },
            stoppingToken);

        if (!IsSuccess(lowStockResponse))
        {
            throw new InvalidOperationException(
                $"GET_LOW_STOCK failed: {GetErrorMessage(lowStockResponse)}");
        }

        var lowStockProducts =
            lowStockResponse.Data.ValueKind == JsonValueKind.Array
                ? lowStockResponse.Data
                    .Deserialize<List<InventoryProduct>>(
                        _jsonOptions)
                    ?? new List<InventoryProduct>()
                : new List<InventoryProduct>();

        _logger.LogInformation(
            "Low-stock products found: {Count}",
            lowStockProducts.Count);

        var restockedProducts = 0;
        var totalRestockedQuantity = 0;

        foreach (var product in lowStockProducts)
        {
            if (product.ReorderLevel <= 0)
            {
                _logger.LogInformation(
                    "Skipping product {ProductId} ({ProductName}) because ReorderLevel is {ReorderLevel}.",
                    product.Id,
                    product.Name,
                    product.ReorderLevel);

                continue;
            }

            var restockQuantity =
                (2 * product.ReorderLevel) -
                product.QuantityInStock;

            if (restockQuantity <= 0)
            {
                _logger.LogInformation(
                    "Skipping product {ProductId} ({ProductName}) because calculated restock quantity is {Quantity}.",
                    product.Id,
                    product.Name,
                    restockQuantity);

                continue;
            }

            var restockResponse = await SendRequestAsync(
                new TcpRequest
                {
                    RequestId = CreateRequestId(),
                    Command = "RESTOCK",
                    Data = new RestockRequest
                    {
                        ProductId = product.Id,
                        Quantity = restockQuantity
                    }
                },
                stoppingToken);

            if (!IsSuccess(restockResponse))
            {
                _logger.LogWarning(
                    "Failed to restock product {ProductId} ({ProductName}): {Error}",
                    product.Id,
                    product.Name,
                    GetErrorMessage(restockResponse));

                continue;
            }

            restockedProducts++;
            totalRestockedQuantity += restockQuantity;

            _logger.LogInformation(
                "Restocked product {ProductId} ({ProductName}) by {Quantity} units.",
                product.Id,
                product.Name,
                restockQuantity);
        }

        var summaryResponse = await SendRequestAsync(
            new TcpRequest
            {
                RequestId = CreateRequestId(),
                Command = "GET_SUMMARY"
            },
            stoppingToken);

        if (!IsSuccess(summaryResponse))
        {
            throw new InvalidOperationException(
                $"GET_SUMMARY failed: {GetErrorMessage(summaryResponse)}");
        }

        var summary =
            summaryResponse.Data.Deserialize<InventorySummary>(
                _jsonOptions)
            ?? throw new InvalidOperationException(
                "Unable to deserialize GET_SUMMARY response.");

        var report =
            $"INVENTORY SUMMARY | " +
            $"Total Products: {summary.TotalProducts} | " +
            $"Total Units: {summary.TotalUnits} | " +
            $"Low Stock Products: {summary.LowStockCount} | " +
            $"Stock Value: {summary.StockValue:0.00} | " +
            $"Restocked Products This Cycle: {restockedProducts} | " +
            $"Units Added This Cycle: {totalRestockedQuantity}";

        _logger.LogInformation(
            "{Report}",
            report);

        await WriteReportToFileAsync(
            report,
            stoppingToken);
    }

    private async Task EnsureConnectedAsync(
        CancellationToken stoppingToken)
    {
        if (_client != null &&
            _client.Connected &&
            _reader != null &&
            _writer != null)
        {
            return;
        }

        Disconnect();

        var host =
            _configuration["TcpClient:Host"]
            ?? "127.0.0.1";

        var port =
            _configuration.GetValue<int?>(
                "TcpClient:Port")
            ?? 5050;

        _logger.LogInformation(
            "Connecting to TCP server {Host}:{Port}...",
            host,
            port);

        var client = new TcpClient();

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken);

        timeoutCts.CancelAfter(
            TimeSpan.FromSeconds(
                GetTimeoutSeconds()));

        try
        {
            await client.ConnectAsync(
                host,
                port,
                timeoutCts.Token);
        }
        catch (OperationCanceledException)
            when (!stoppingToken.IsCancellationRequested)
        {
            client.Dispose();

            throw new TimeoutException(
                $"TCP connection to {host}:{port} timed out.");
        }

        var stream = client.GetStream();

        _client = client;

        _reader = new StreamReader(
            stream,
            new UTF8Encoding(false),
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        _writer = new StreamWriter(
            stream,
            new UTF8Encoding(false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        _logger.LogInformation(
            "Connected to TCP server {Host}:{Port}.",
            host,
            port);
    }

    private async Task<TcpResponse> SendRequestAsync(
        TcpRequest request,
        CancellationToken stoppingToken)
    {
        await EnsureConnectedAsync(
            stoppingToken);

        if (_writer == null ||
            _reader == null)
        {
            throw new IOException(
                "TCP connection is not available.");
        }

        var requestJson =
            JsonSerializer.Serialize(
                request,
                _jsonOptions);

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken);

        timeoutCts.CancelAfter(
            TimeSpan.FromSeconds(
                GetTimeoutSeconds()));

        try
        {
            await _writer.WriteLineAsync(
                requestJson.AsMemory(),
                timeoutCts.Token);

            var responseLine =
                await _reader.ReadLineAsync(
                    timeoutCts.Token);

            if (responseLine == null)
            {
                throw new IOException(
                    "TCP server closed the connection.");
            }

            var response =
                JsonSerializer.Deserialize<TcpResponse>(
                    responseLine,
                    _jsonOptions);

            if (response == null)
            {
                throw new JsonException(
                    "TCP server returned an empty response.");
            }

            if (!string.Equals(
                    response.RequestId,
                    request.RequestId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "TCP response requestId does not match the request.");
            }

            return response;
        }
        catch (OperationCanceledException)
            when (!stoppingToken.IsCancellationRequested)
        {
            Disconnect();

            throw new TimeoutException(
                $"TCP request '{request.Command}' timed out.");
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    private async Task WriteReportToFileAsync(
        string report,
        CancellationToken stoppingToken)
    {
        var logDirectory =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "logs");

        Directory.CreateDirectory(
            logDirectory);

        var logFile =
            Path.Combine(
                logDirectory,
                "inventory-report.log");

        var line =
            $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} | {report}{Environment.NewLine}";

        await File.AppendAllTextAsync(
            logFile,
            line,
            Encoding.UTF8,
            stoppingToken);

        _logger.LogInformation(
            "Inventory report written to {LogFile}.",
            logFile);
    }

    private int GetTimeoutSeconds()
    {
        return _configuration.GetValue<int?>(
                   "TcpClient:TimeoutSeconds")
               ?? 5;
    }

    private int GetCycleIntervalSeconds()
    {
        return _configuration.GetValue<int?>(
                   "Worker:CycleIntervalSeconds")
               ?? 30;
    }

    private void Disconnect()
    {
        try
        {
            _writer?.Dispose();
        }
        catch
        {
        }

        try
        {
            _reader?.Dispose();
        }
        catch
        {
        }

        try
        {
            _client?.Dispose();
        }
        catch
        {
        }

        _writer = null;
        _reader = null;
        _client = null;
    }

    private static bool IsSuccess(
        TcpResponse response)
    {
        return string.Equals(
            response.Status,
            "ok",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetErrorMessage(
        TcpResponse response)
    {
        if (response.Error == null)
        {
            return "Unknown TCP error.";
        }

        return $"{response.Error.Code}: {response.Error.Message}";
    }

    private static string CreateRequestId()
    {
        return Guid.NewGuid()
            .ToString("N");
    }

    private sealed class TcpRequest
    {
        public string RequestId { get; set; } =
            string.Empty;

        public string Command { get; set; } =
            string.Empty;

        public object? Data { get; set; }
    }

    private sealed class TcpResponse
    {
        public string RequestId { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public JsonElement Data { get; set; }

        public TcpError? Error { get; set; }
    }

    private sealed class TcpError
    {
        public string Code { get; set; } =
            string.Empty;

        public string Message { get; set; } =
            string.Empty;
    }

    private sealed class RestockRequest
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }

    private sealed class InventoryProduct
    {
        public int Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public int QuantityInStock { get; set; }

        public int ReorderLevel { get; set; }
    }

    private sealed class InventorySummary
    {
        public int TotalProducts { get; set; }

        public int TotalUnits { get; set; }

        public int LowStockCount { get; set; }

        public decimal StockValue { get; set; }
    }
}