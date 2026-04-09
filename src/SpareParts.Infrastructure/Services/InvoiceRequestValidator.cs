using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Services;

public static class InvoiceRequestValidator
{
    public static void ValidateSaleItems(IList<SaleItemDto>? items, string emptyMessage = "Invoice must have at least one item.")
    {
        if (items == null || items.Count == 0)
        {
            throw new ValidationException(emptyMessage);
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var lineNumber = index + 1;

            if (item.PartId <= 0)
            {
                throw new ValidationException($"Sale line {lineNumber} must reference a valid part.");
            }

            if (item.Quantity <= 0)
            {
                throw new ValidationException($"Sale line {lineNumber} quantity must be greater than zero.");
            }

            if (item.UnitPrice < 0)
            {
                throw new ValidationException($"Sale line {lineNumber} unit price cannot be negative.");
            }

            if (item.DiscountAmount < 0)
            {
                throw new ValidationException($"Sale line {lineNumber} discount cannot be negative.");
            }

            if (item.TaxRate < 0)
            {
                throw new ValidationException($"Sale line {lineNumber} tax rate cannot be negative.");
            }

            var lineSubtotal = item.Quantity * item.UnitPrice;
            if (item.DiscountAmount > lineSubtotal)
            {
                throw new ValidationException($"Sale line {lineNumber} discount cannot exceed the line subtotal.");
            }
        }
    }

    public static void ValidatePurchaseItems(IList<PurchaseItemDto>? items, string emptyMessage = "Purchase must have at least one item.")
    {
        if (items == null || items.Count == 0)
        {
            throw new ValidationException(emptyMessage);
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var lineNumber = index + 1;

            if (item.PartId <= 0)
            {
                throw new ValidationException($"Purchase line {lineNumber} must reference a valid part.");
            }

            if (item.Quantity <= 0)
            {
                throw new ValidationException($"Purchase line {lineNumber} quantity must be greater than zero.");
            }

            if (item.UnitCost < 0)
            {
                throw new ValidationException($"Purchase line {lineNumber} unit cost cannot be negative.");
            }

            if (item.TaxRate < 0)
            {
                throw new ValidationException($"Purchase line {lineNumber} tax rate cannot be negative.");
            }
        }
    }

    public static Dictionary<int, int> AggregateSaleQuantities(IEnumerable<SaleItemDto> items)
        => items
            .GroupBy(item => item.PartId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
}
