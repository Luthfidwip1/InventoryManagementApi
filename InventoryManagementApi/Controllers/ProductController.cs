using InventoryManagementApi.DTOs.Products;
using InventoryManagementApi.DTOs.StockTransactions;
using InventoryManagementApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly StockService _stockService;

    public ProductsController(
        ProductService productService,
        StockService stockService)
    {
        _productService = productService;
        _stockService = stockService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(List<ProductResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ProductResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Pagination",
                detail: "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Pagination",
                detail: "PageSize must be between 1 and 50.");
        }

        var products = await _productService.GetAllAsync(
            search,
            categoryId,
            page,
            pageSize);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Product Not Found",
                detail: "Product not found.");
        }

        return Ok(product);
    }

    [HttpGet("{id:int}/transactions")]
    [ProducesResponseType(
        typeof(List<StockTransactionResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<StockTransactionResponse>>>
        GetTransactions(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Product Not Found",
                detail: "Product not found.");
        }

        var transactions =
            await _stockService.GetByProductIdAsync(id);

        return Ok(transactions);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ProductResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request)
    {
        try
        {
            var product = await _productService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "Category not found.")
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Category",
                    detail: ex.Message);
            }

            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Product Conflict",
                detail: ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        UpdateProductRequest request)
    {
        try
        {
            var product = await _productService.UpdateAsync(
                id,
                request);

            if (product == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Product Not Found",
                    detail: "Product not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "Category not found.")
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Category",
                    detail: ex.Message);
            }

            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Product Conflict",
                detail: ex.Message);
        }
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(
        typeof(List<ProductResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductResponse>>> GetLowStock()
    {
        var products = await _productService.GetLowStockAsync();

        return Ok(products);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _productService.DeleteAsync(id);

            if (!deleted)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Product Not Found",
                    detail: "Product not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Product Conflict",
                detail: ex.Message);
        }
    }
}