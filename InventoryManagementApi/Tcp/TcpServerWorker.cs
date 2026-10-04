using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using InventoryManagementApi.DTOs.StockTransactions;
using InventoryManagementApi.Enums;
using InventoryManagementApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagementApi.Tcp;

public class TcpServerWorker : BackgroundService
{
    private const int MaxLineLength = 8 * 1024;

    private readonly ILogger<TcpServerWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    private TcpListener? _listener;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

    public TcpServerWorker(
        ILogger<TcpServerWorker> logger,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var port = _configuration.GetValue<int?>(
            "TcpServer:Port") ?? 5050;

        _listener = new TcpListener(
            IPAddress.Any,
            port);

        _listener.Start();

        _logger.LogInformation(
            "TCP server started on port {Port}.",
            port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await _listener.AcceptTcpClientAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                _ = Task.Run(
                    () => HandleClientAsync(
                        client,
                        stoppingToken),
                    CancellationToken.None);
            }
        }
        finally
        {
            _listener.Stop();

            _logger.LogInformation(
                "TCP server stopped.");
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken stoppingToken)
    {
        var remoteEndPoint =
            client.Client.RemoteEndPoint?.ToString()
            ?? "unknown";

        _logger.LogInformation(
            "TCP client connected: {RemoteEndPoint}",
            remoteEndPoint);

        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(
                       stream,
                       new UTF8Encoding(false),
                       detectEncodingFromByteOrderMarks: false,
                       bufferSize: 4096,
                       leaveOpen: true))
            using (var writer = new StreamWriter(
                       stream,
                       new UTF8Encoding(false),
                       bufferSize: 4096,
                       leaveOpen: true)
            {
                AutoFlush = true
            })
            {
                while (!stoppingToken.IsCancellationRequested &&
                       client.Connected)
                {
                    string? line;

                    try
                    {
                        line = await reader.ReadLineAsync(
                            stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    if (line == null)
                    {
                        break;
                    }

                    if (line.Length > MaxLineLength)
                    {
                        _logger.LogWarning(
                            "TCP client {RemoteEndPoint} sent a line longer than 8 KB. Closing connection.",
                            remoteEndPoint);

                        break;
                    }

                    var response =
                        await ProcessMessageAsync(
                            line,
                            stoppingToken);

                    var responseJson =
                        JsonSerializer.Serialize(
                            response,
                            _jsonOptions);

                    await writer.WriteLineAsync(
                        responseJson);
                }
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(
                ex,
                "TCP connection error from {RemoteEndPoint}.",
                remoteEndPoint);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(
                ex,
                "TCP socket error from {RemoteEndPoint}.",
                remoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected TCP client error from {RemoteEndPoint}.",
                remoteEndPoint);
        }
        finally
        {
            _logger.LogInformation(
                "TCP client disconnected: {RemoteEndPoint}",
                remoteEndPoint);
        }
    }

    private async Task<TcpResponse> ProcessMessageAsync(
        string message,
        CancellationToken stoppingToken)
    {
        TcpRequest? request;

        try
        {
            request = JsonSerializer.Deserialize<TcpRequest>(
                message,
                _jsonOptions);
        }
        catch (JsonException)
        {
            return TcpResponse.Fail(
                string.Empty,
                "BAD_REQUEST",
                "Request is not valid JSON.");
        }

        if (request == null)
        {
            return TcpResponse.Fail(
                string.Empty,
                "BAD_REQUEST",
                "Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.RequestId))
        {
            return TcpResponse.Fail(
                string.Empty,
                "BAD_REQUEST",
                "requestId is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Command))
        {
            return TcpResponse.Fail(
                request.RequestId,
                "BAD_REQUEST",
                "command is required.");
        }

        var command = request.Command
            .Trim()
            .ToUpperInvariant();

        switch (command)
        {
            case "PING":
                return TcpResponse.Ok(
                    request.RequestId,
                    "PONG");

            case "GET_LOW_STOCK":
                return await HandleGetLowStockAsync(
                    request.RequestId,
                    stoppingToken);

            case "RESTOCK":
                return await HandleRestockAsync(
                    request,
                    stoppingToken);

            case "GET_SUMMARY":
                return await HandleGetSummaryAsync(
                    request.RequestId,
                    stoppingToken);

            default:
                return TcpResponse.Fail(
                    request.RequestId,
                    "UNKNOWN_COMMAND",
                    $"Unknown command: {request.Command}");
        }
    }

    private async Task<TcpResponse> HandleGetLowStockAsync(
        string requestId,
        CancellationToken stoppingToken)
    {
        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var productService =
                scope.ServiceProvider
                    .GetRequiredService<ProductService>();

            var products =
                await productService.GetLowStockAsync();

            stoppingToken.ThrowIfCancellationRequested();

            return TcpResponse.Ok(
                requestId,
                products);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process GET_LOW_STOCK.");

            return TcpResponse.Fail(
                requestId,
                "INTERNAL_ERROR",
                "Failed to retrieve low-stock products.");
        }
    }

    private async Task<TcpResponse> HandleRestockAsync(
        TcpRequest request,
        CancellationToken stoppingToken)
    {
        if (request.Data == null)
        {
            return TcpResponse.Fail(
                request.RequestId,
                "BAD_REQUEST",
                "data is required.");
        }

        TcpRestockData? restockData;

        try
        {
            restockData =
                request.Data.Value.Deserialize<TcpRestockData>(
                    _jsonOptions);
        }
        catch (JsonException)
        {
            return TcpResponse.Fail(
                request.RequestId,
                "BAD_REQUEST",
                "Invalid RESTOCK data.");
        }

        if (restockData == null)
        {
            return TcpResponse.Fail(
                request.RequestId,
                "BAD_REQUEST",
                "Invalid RESTOCK data.");
        }

        if (restockData.Quantity <= 0)
        {
            return TcpResponse.Fail(
                request.RequestId,
                "INVALID_QUANTITY",
                "Quantity must be greater than zero.");
        }

        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var productService =
                scope.ServiceProvider
                    .GetRequiredService<ProductService>();

            var stockService =
                scope.ServiceProvider
                    .GetRequiredService<StockService>();

            var product =
                await productService.GetByIdAsync(
                    restockData.ProductId);

            if (product == null)
            {
                return TcpResponse.Fail(
                    request.RequestId,
                    "PRODUCT_NOT_FOUND",
                    "Product not found.");
            }

            var stockRequest =
                new CreateStockTransactionRequest
                {
                    ProductId = restockData.ProductId,
                    Type = StockTransactionType.In,
                    Quantity = restockData.Quantity,
                    Note = "Automatic restock by worker"
                };

            var transaction =
                await stockService.CreateAsync(
                    stockRequest,
                    StockTransactionSource.Worker);

            stoppingToken.ThrowIfCancellationRequested();

            return TcpResponse.Ok(
                request.RequestId,
                transaction);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "Product not found.")
            {
                return TcpResponse.Fail(
                    request.RequestId,
                    "PRODUCT_NOT_FOUND",
                    ex.Message);
            }

            if (ex.Message.Contains(
                    "quantity",
                    StringComparison.OrdinalIgnoreCase))
            {
                return TcpResponse.Fail(
                    request.RequestId,
                    "INVALID_QUANTITY",
                    ex.Message);
            }

            _logger.LogWarning(
                ex,
                "RESTOCK validation failed.");

            return TcpResponse.Fail(
                request.RequestId,
                "BAD_REQUEST",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process RESTOCK.");

            return TcpResponse.Fail(
                request.RequestId,
                "INTERNAL_ERROR",
                "Failed to restock product.");
        }
    }

    private async Task<TcpResponse> HandleGetSummaryAsync(
        string requestId,
        CancellationToken stoppingToken)
    {
        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var productService =
                scope.ServiceProvider
                    .GetRequiredService<ProductService>();

            var summary =
                await productService.GetSummaryAsync();

            stoppingToken.ThrowIfCancellationRequested();

            return TcpResponse.Ok(
                requestId,
                summary);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process GET_SUMMARY.");

            return TcpResponse.Fail(
                requestId,
                "INTERNAL_ERROR",
                "Failed to retrieve inventory summary.");
        }
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        _listener?.Stop();

        return base.StopAsync(
            cancellationToken);
    }
}