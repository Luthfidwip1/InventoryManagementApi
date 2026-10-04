using System.ComponentModel.DataAnnotations;

namespace InventoryManagementApi.DTOs.Categories;

public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Name cannot be blank.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(
        500,
        ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}