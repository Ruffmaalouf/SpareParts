using Dapper;
using SpareParts.Domain.Sales;
using SpareParts.Domain.WebCatalog;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class WebCatalogService
{
    private const string AreebaPaymentMethod = "Areeba Gateway";
    private const string WhishPaymentMethod = "Whish Gateway";

    private readonly ISqlConnectionFactory _factory;
    private readonly SalesService _salesService;

    public WebCatalogService(ISqlConnectionFactory factory, SalesService salesService)
    {
        _factory = factory;
        _salesService = salesService;
    }

    public IReadOnlyList<WebCatalogPartDto> GetAvailableParts(string? search, int page, int pageSize)
    {
        var warehouse = ResolveCheckoutWarehouse();
        using var conn = _factory.CreateConnection();
        return conn.Query<WebCatalogPartDto>(
            """
SELECT p.Id,
       p.InternalCode,
       p.Barcode,
       p.Name,
       p.OEMNumber,
       CONVERT(NVARCHAR(40), p.Condition) AS Condition,
       p.SalePrice,
       p.Currency,
       p.Notes,
       s.Quantity AS AvailableQuantity,
       w.Id AS WarehouseId,
       w.Name AS WarehouseName
FROM dbo.Parts p
INNER JOIN dbo.Stock s ON s.PartId = p.Id AND s.WarehouseId = @WarehouseId
INNER JOIN dbo.Warehouses w ON w.Id = s.WarehouseId
WHERE p.IsActive = 1
  AND s.Quantity > 0
  AND (@Search IS NULL OR @Search = N''
       OR p.InternalCode LIKE N'%' + @Search + N'%'
       OR p.Name LIKE N'%' + @Search + N'%'
       OR p.OEMNumber LIKE N'%' + @Search + N'%'
       OR p.Barcode LIKE N'%' + @Search + N'%')
ORDER BY p.Name
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
""",
            new
            {
                WarehouseId = warehouse.Id,
                Search = search?.Trim(),
                Skip = (page - 1) * pageSize,
                Take = pageSize
            }).ToList();
    }

    public WebCheckoutResponse Checkout(WebCheckoutRequest request, int userId)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new ValidationException("Cart is empty.");
        }

        var warehouse = ResolveCheckoutWarehouse();
        using var conn = _factory.CreateConnection();
        var partIds = request.Items.Select(item => item.PartId).Distinct().ToArray();
        var parts = conn.Query<WebCheckoutPartRow>(
            """
SELECT p.Id,
       p.SalePrice,
       p.Currency,
       p.CostPrice,
       s.Quantity AS AvailableQuantity
FROM dbo.Parts p
INNER JOIN dbo.Stock s ON s.PartId = p.Id AND s.WarehouseId = @WarehouseId
WHERE p.IsActive = 1
  AND s.Quantity > 0
  AND p.Id IN @PartIds;
""",
            new { WarehouseId = warehouse.Id, PartIds = partIds })
            .ToDictionary(part => part.Id);

        var saleItems = new List<SaleItemDto>();
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new ValidationException("Cart quantities must be greater than zero.");
            }

            if (!parts.TryGetValue(item.PartId, out var part))
            {
                throw new NotFoundException($"Part {item.PartId} is not available for checkout.");
            }

            if (part.AvailableQuantity < item.Quantity)
            {
                throw new ConflictException($"Not enough stock for part {item.PartId}. Available: {part.AvailableQuantity}");
            }

            saleItems.Add(new SaleItemDto
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitPrice = part.SalePrice,
                DiscountAmount = 0m,
                TaxRate = 0m
            });
        }

        var paymentMethod = ResolvePaymentMethod(request.PaymentMethod);
        var saleRequest = new CreateSaleRequest
        {
            InvoiceDate = DateTime.Today,
            WarehouseId = warehouse.Id,
            PaymentMethod = paymentMethod,
            PaidAmount = 0m,
            Notes = BuildCheckoutNotes(request),
            Items = saleItems
        };

        var result = _salesService.CreateSale(saleRequest, userId);
        return new WebCheckoutResponse
        {
            InvoiceId = result.InvoiceId,
            InvoiceNumber = result.InvoiceNumber,
            TotalAmount = result.TotalAmount,
            PaymentStatus = result.PaymentStatus,
            WarehouseId = warehouse.Id
        };
    }

    private CheckoutWarehouse ResolveCheckoutWarehouse()
    {
        using var conn = _factory.CreateConnection();
        var configuredWarehouseId = conn.ExecuteScalar<int?>(
            """
SELECT TOP (1) TRY_CONVERT(INT, [Value])
FROM dbo.AppConstants
WHERE [Key] = N'WebCheckoutWarehouseId';
""");

        var warehouse = conn.QueryFirstOrDefault<CheckoutWarehouse>(
            """
SELECT TOP (1) Id, Name
FROM dbo.Warehouses
WHERE (@WarehouseId IS NULL OR Id = @WarehouseId)
ORDER BY CASE WHEN @WarehouseId IS NOT NULL AND Id = @WarehouseId THEN 0 ELSE 1 END,
         IsMain DESC,
         Id;
""",
            new { WarehouseId = configuredWarehouseId });

        return warehouse ?? throw new ValidationException("No checkout warehouse is configured.");
    }

    private static string ResolvePaymentMethod(string? paymentMethod)
    {
        var normalized = paymentMethod?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Web Checkout";
        }

        return normalized.ToLowerInvariant() switch
        {
            "areeba" or "areeba gateway" or "areeba_gateway" => AreebaPaymentMethod,
            "whish" or "whish gateway" or "whish_gateway" => WhishPaymentMethod,
            _ => throw new ValidationException("Unsupported web checkout payment method.")
        };
    }

    private static string BuildCheckoutNotes(WebCheckoutRequest request)
    {
        var lines = new List<string> { "Web checkout order" };
        if (!string.IsNullOrWhiteSpace(request.CustomerName)) lines.Add($"Name: {request.CustomerName.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.CustomerPhone)) lines.Add($"Phone: {request.CustomerPhone.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail)) lines.Add($"Email: {request.CustomerEmail.Trim()}");
        var paymentMethod = ResolvePaymentMethod(request.PaymentMethod);
        lines.Add($"Payment method: {paymentMethod}");
        if (!string.IsNullOrWhiteSpace(request.PaymentReference)) lines.Add($"Payment reference: {request.PaymentReference.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.ShippingAddressLine1)) lines.Add($"Shipping address 1: {request.ShippingAddressLine1.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.ShippingAddressLine2)) lines.Add($"Shipping address 2: {request.ShippingAddressLine2.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.ShippingCity)) lines.Add($"Shipping city: {request.ShippingCity.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.ShippingRegion)) lines.Add($"Shipping region: {request.ShippingRegion.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.ShippingPostalCode)) lines.Add($"Shipping postal code: {request.ShippingPostalCode.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.ShippingCountry)) lines.Add($"Shipping country: {request.ShippingCountry.Trim()}");
        if (!string.IsNullOrWhiteSpace(request.DeliveryInstructions)) lines.Add($"Delivery instructions: {request.DeliveryInstructions.Trim()}");
        return string.Join(Environment.NewLine, lines);
    }

    private sealed class CheckoutWarehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class WebCheckoutPartRow
    {
        public int Id { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal CostPrice { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
