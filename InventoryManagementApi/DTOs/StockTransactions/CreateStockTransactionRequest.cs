using System.ComponentModel.DataAnnotations;
using InventoryManagementApi.Enums;

namespace InventoryManagementApi.DTOs.StockTransactions;

public class CreateStockTransactionRequest : IValidatableObject
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "ProductId must be greater than 0.")]
    public int ProductId { get; set; }

    public StockTransactionType Type { get; set; }

    public int Quantity { get; set; }

    [MaxLength(
        250,
        ErrorMessage = "Note cannot exceed 250 characters.")]
    public string? Note { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(StockTransactionType), Type))
        {
            yield return new ValidationResult(
                "Invalid transaction type.",
                new[] { nameof(Type) });

            yield break;
        }

        if (Quantity == 0)
        {
            yield return new ValidationResult(
                "Quantity cannot be zero.",
                new[] { nameof(Quantity) });
        }

        if (Type == StockTransactionType.In && Quantity < 0)
        {
            yield return new ValidationResult(
                "In transaction quantity must be positive.",
                new[] { nameof(Quantity) });
        }

        if (Type == StockTransactionType.Out && Quantity < 0)
        {
            yield return new ValidationResult(
                "Out transaction quantity must be positive.",
                new[] { nameof(Quantity) });
        }
    }
}