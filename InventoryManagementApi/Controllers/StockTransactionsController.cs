using InventoryManagementApi.DTOs.StockTransactions;
using InventoryManagementApi.Enums;
using InventoryManagementApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockTransactionsController : ControllerBase
{
    private readonly StockService _stockService;

    public StockTransactionsController(StockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(List<StockTransactionResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StockTransactionResponse>>> GetAll(
        [FromQuery] int? productId,
        [FromQuery] StockTransactionType? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] StockTransactionSource? source)
    {
        var transactions = await _stockService.GetAllAsync(
            productId,
            type,
            from,
            to,
            source);

        return Ok(transactions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(StockTransactionResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockTransactionResponse>> GetById(
        int id)
    {
        var transaction = await _stockService.GetByIdAsync(id);

        if (transaction == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Stock Transaction Not Found",
                detail: "Stock transaction not found.");
        }

        return Ok(transaction);
    }

    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(
        typeof(List<StockTransactionResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StockTransactionResponse>>>
        GetByProductId(int productId)
    {
        var transactions =
            await _stockService.GetByProductIdAsync(productId);

        return Ok(transactions);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(StockTransactionResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockTransactionResponse>> Create(
        CreateStockTransactionRequest request)
    {
        try
        {
            var transaction = await _stockService.CreateAsync(
                request,
                StockTransactionSource.Api);

            return CreatedAtAction(
                nameof(GetById),
                new { id = transaction.Id },
                transaction);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Stock Transaction Error",
                detail: ex.Message);
        }
    }

    [HttpPost("worker")]
    [ProducesResponseType(
        typeof(StockTransactionResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockTransactionResponse>>
        CreateFromWorker(CreateStockTransactionRequest request)
    {
        try
        {
            var transaction = await _stockService.CreateAsync(
                request,
                StockTransactionSource.Worker);

            return CreatedAtAction(
                nameof(GetById),
                new { id = transaction.Id },
                transaction);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Stock Transaction Error",
                detail: ex.Message);
        }
    }
}