using SpareParts.Domain.Inventory;

namespace SpareParts.Api.Notifications;

public sealed record PartAddedNotification(
    int Id,
    string InternalCode,
    string Name,
    decimal SalePrice,
    string Currency,
    DateTimeOffset CreatedAt)
{
    public static PartAddedNotification From(int id, CreatePartRequest request)
        => new(
            id,
            request.InternalCode?.Trim() ?? string.Empty,
            request.Name?.Trim() ?? string.Empty,
            request.SalePrice,
            string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim(),
            DateTimeOffset.UtcNow);
}
