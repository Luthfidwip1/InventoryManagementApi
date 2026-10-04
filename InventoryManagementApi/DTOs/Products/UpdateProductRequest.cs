using System.ComponentModel.DataAnnotations;

namespace InventoryManagementApi.DTOs.Products;

public class UpdateProductRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "CategoryId must be greater than 0.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "SKU is required.")]
    [MaxLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "SKU cannot be blank.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Name cannot be blank.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(
        500,
        ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "UnitPrice cannot be negative.")]
    public decimal UnitPrice { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "ReorderLevel cannot be negative.")]
    public int ReorderLevel { get; set; }
}