using Dapper;
using SpareParts.Domain.Sales;
using SpareParts.Domain.WebCatalog;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;
using System.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class WebCatalogService
{
    private const string AreebaPaymentMethod = "Areeba Gateway";
    private const string WhishPaymentMethod = "Whish Gateway";

    private readonly ISqlConnectionFactory _factory;
    private readonly SalesService _salesService;
    private readonly ITenantContext _tenantContext;
    private readonly PublicCatalogOptions _publicCatalogOptions;

    public WebCatalogService(
        ISqlConnectionFactory factory,
        SalesService salesService,
        ITenantContext tenantContext,
        PublicCatalogOptions? publicCatalogOptions = null)
    {
        _factory = factory;
        _salesService = salesService;
        _tenantContext = tenantContext;
        _publicCatalogOptions = publicCatalogOptions ?? new PublicCatalogOptions();
    }

    /// <summary>
    /// Resolves the tenant to scope catalog/checkout reads to. Authenticated, tenant-resolved callers
    /// (TenantId &gt; 0) and SuperAdmins (TenantId == 0 meaning "all tenants") are unchanged from before.
    /// The one new case: an anonymous request that TenantResolutionMiddleware never touched — TenantId
    /// is still exactly 0 and IsResolved is still false — now resolves to the configured public
    /// storefront tenant (<see cref="PublicCatalogOptions.TenantId"/>) instead of being indistinguishable
    /// from "no tenant". Anything else non-positive (e.g. a negative/corrupted TenantId) is left alone and
    /// still fails closed in the callers below — it is never the shape of a genuine anonymous request.
    /// </summary>
    private int ResolveTenantId()
    {
        if (_tenantContext.IsSuperAdmin)
        {
            return _tenantContext.TenantId;
        }

        if (_tenantContext.TenantId > 0)
        {
            return _tenantContext.TenantId;
        }

        if (_tenantContext.TenantId == 0 && !_tenantContext.IsResolved && _publicCatalogOptions.TenantId > 0)
        {
            return _publicCatalogOptions.TenantId;
        }

        return _tenantContext.TenantId;
    }

    public IReadOnlyList<WebCatalogPartDto> GetAvailableParts(string? search, int page, int pageSize)
    {
        var tenantId = ResolveTenantId();

        // Anonymous public-catalog callers must resolve to a specific tenant. TenantId == 0 only means
        // "see all tenants" for an authenticated SuperAdmin — for everyone else (including unauthenticated
        // requests that could not be resolved to the configured public-catalog tenant above) we fail
        // closed and return no data instead of leaking every tenant's parts/pricing/stock.
        if (!_tenantContext.IsSuperAdmin && tenantId <= 0)
        {
            return [];
        }

        var warehouse = ResolveCheckoutWarehouse(tenantId);
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
       w.Name AS WarehouseName,
       p.UsedCarId,
       p.ImageUrls,
       CASE WHEN ISJSON(p.ImageUrls) = 1 THEN JSON_VALUE(p.ImageUrls, '$[0]') ELSE NULL END AS ImageUrl,
       CASE
           WHEN uc.Id IS NULL THEN N'Warehouse stock'
           ELSE CONCAT(cb.Name, N' ', cm.Name, N' / ', uc.ModelYear)
       END AS DonorCar,
       COALESCE(NULLIF(LTRIM(RTRIM(loc.Name)), N''), NULLIF(LTRIM(RTRIM(uc.Location)), N''), w.Name) AS StorageLocation
FROM dbo.Parts p
INNER JOIN dbo.Stock s ON s.PartId = p.Id AND s.WarehouseId = @WarehouseId
INNER JOIN dbo.Warehouses w ON w.Id = s.WarehouseId
LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
WHERE p.IsActive = 1
  AND s.Quantity > 0
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
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
                TenantId = tenantId,
                Search = search?.Trim(),
                Skip = (page - 1) * pageSize,
                Take = pageSize
            }).ToList();
    }

    public WebCheckoutResponse Checkout(WebCheckoutRequest request, int userId)
    {
        var priced = PriceCart(request.Items, request.PromoCode);

        var paymentMethod = ResolvePaymentMethod(request.PaymentMethod);
        var saleRequest = new CreateSaleRequest
        {
            InvoiceDate = DateTime.Today,
            WarehouseId = priced.Warehouse.Id,
            PaymentMethod = paymentMethod,
            PaidAmount = 0m,
            Notes = BuildCheckoutNotes(request),
            Items = priced.Items.ToList()
        };

        var result = _salesService.CreateSale(saleRequest, userId);
        return new WebCheckoutResponse
        {
            InvoiceId = result.InvoiceId,
            InvoiceNumber = result.InvoiceNumber,
            Subtotal = priced.Subtotal,
            DiscountAmount = priced.DiscountAmount,
            TotalAmount = result.TotalAmount,
            PaymentStatus = result.PaymentStatus,
            WarehouseId = priced.Warehouse.Id
        };
    }

    /// <summary>
    /// Read-only price preview for POST /api/web-catalog/checkout/quote. Prices the cart and
    /// validates the promo code using the exact same logic as <see cref="Checkout"/> (via
    /// <see cref="PriceCart"/>), but never calls SalesService/CreateSale — no invoice row, no stock
    /// movement, no persistence of any kind. This is what lets the storefront's "Apply" button check
    /// a promo code before the shopper confirms a real order.
    /// </summary>
    public WebCheckoutQuoteResponse Quote(WebCheckoutQuoteRequest request)
    {
        var priced = PriceCart(request.Items, request.PromoCode);

        return new WebCheckoutQuoteResponse
        {
            Subtotal = priced.Subtotal,
            DiscountAmount = priced.DiscountAmount,
            DiscountPercent = priced.DiscountPercent,
            TotalAmount = priced.Subtotal - priced.DiscountAmount
        };
    }

    /// <summary>
    /// Loads and prices the requested parts (server-side prices only, never trusting a browser-
    /// supplied price), validates stock availability, and applies the promo code — shared, read-only
    /// groundwork for both <see cref="Checkout"/> (which persists a real sale from the result) and
    /// <see cref="Quote"/> (which returns the numbers only). Guards the same anonymous/unresolved-
    /// tenant shape as GetAvailableParts before making any database call: Checkout can only reach
    /// this with an authenticated, resolved tenant (its endpoint requires login), but Quote is
    /// intentionally anonymous, so this guard is what keeps an anonymous quote request from ever
    /// pricing another tenant's parts if the public-catalog tenant is left unconfigured.
    /// </summary>
    private PricedCart PriceCart(List<WebCheckoutItemDto>? items, string? promoCode)
    {
        if (items == null || items.Count == 0)
        {
            throw new ValidationException("Cart is empty.");
        }

        var tenantId = ResolveTenantId();
        if (!_tenantContext.IsSuperAdmin && tenantId <= 0)
        {
            throw new ValidationException("Public storefront pricing is not available for this request.");
        }

        var warehouse = ResolveCheckoutWarehouse(tenantId);
        using var conn = _factory.CreateConnection();
        var partIds = items.Select(item => item.PartId).Distinct().ToArray();
        var parts = conn.Query<WebCheckoutPartRow>(
            """
SELECT p.Id,
       p.SalePrice,
       p.Currency,
       p.CostPrice,
       s.Quantity AS AvailableQuantity
FROM dbo.Parts p
INNER JOIN @PartIds ids ON ids.Id = p.Id
INNER JOIN dbo.Stock s ON s.PartId = p.Id AND s.WarehouseId = @WarehouseId
WHERE p.IsActive = 1
  AND s.Quantity > 0
  AND (@TenantId = 0 OR p.TenantId = @TenantId);
""",
            new
            {
                WarehouseId = warehouse.Id,
                TenantId = tenantId,
                PartIds = ToIntIdList(partIds).AsTableValuedParameter("dbo.IntIdList")
            })
            .ToDictionary(part => part.Id);

        var saleItems = new List<SaleItemDto>();
        foreach (var item in items)
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

        var subtotal = Round2(saleItems.Sum(item => item.Quantity * item.UnitPrice));
        var discountAmount = ApplyPromoCode(saleItems, subtotal, promoCode);
        var discountPercent = discountAmount > 0m ? PromoCodeSettings.DiscountRate * 100m : 0m;

        return new PricedCart(saleItems, subtotal, discountAmount, discountPercent, tenantId, warehouse);
    }

    /// <summary>
    /// Validates the promo code (see <see cref="PromoCodeSettings"/>) and, if valid, distributes the
    /// resulting order-level discount across each sale item's DiscountAmount pro-rata to its line
    /// subtotal, so the persisted invoice's own totals (computed independently by
    /// IInvoiceTotalsCalculator from these same items) come out exactly subtotal - discount. The
    /// discount is always computed here, server-side, from <see cref="PromoCodeSettings.DiscountRate"/>
    /// — the request's PromoCode is only ever a string, never a browser-supplied amount.
    /// </summary>
    private static decimal ApplyPromoCode(List<SaleItemDto> saleItems, decimal subtotal, string? promoCode)
    {
        if (string.IsNullOrWhiteSpace(promoCode))
        {
            return 0m;
        }

        var normalizedCode = promoCode.Trim();
        if (!PromoCodeSettings.IsEnabled ||
            !string.Equals(normalizedCode, PromoCodeSettings.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"Promo code '{normalizedCode}' is not valid.");
        }

        var discountAmount = Round2(subtotal * PromoCodeSettings.DiscountRate);
        if (discountAmount <= 0m || subtotal <= 0m)
        {
            return 0m;
        }

        var allocated = 0m;
        for (var i = 0; i < saleItems.Count; i++)
        {
            var item = saleItems[i];
            var lineSubtotal = Round2(item.Quantity * item.UnitPrice);
            var lineDiscount = i == saleItems.Count - 1
                ? Round2(discountAmount - allocated)
                : Round2(discountAmount * (lineSubtotal / subtotal));

            item.DiscountAmount = lineDiscount;
            allocated += lineDiscount;
        }

        return discountAmount;
    }

    private static decimal Round2(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private CheckoutWarehouse ResolveCheckoutWarehouse(int tenantId)
    {
        using var conn = _factory.CreateConnection();
        var configuredWarehouseId = conn.ExecuteScalar<int?>(
            """
SELECT TOP (1) TRY_CONVERT(INT, [Value])
FROM dbo.AppConstants
WHERE [Key] = N'WebCheckoutWarehouseId'
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { TenantId = tenantId });

        var warehouse = conn.QueryFirstOrDefault<CheckoutWarehouse>(
            """
SELECT TOP (1) Id, Name
FROM dbo.Warehouses
WHERE (@WarehouseId IS NULL OR Id = @WarehouseId)
  AND (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY CASE WHEN @WarehouseId IS NOT NULL AND Id = @WarehouseId THEN 0 ELSE 1 END,
         IsMain DESC,
         Id;
""",
            new { WarehouseId = configuredWarehouseId, TenantId = tenantId });

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

    private static DataTable ToIntIdList(IEnumerable<int> ids)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        foreach (var id in ids.Distinct())
        {
            table.Rows.Add(id);
        }

        return table;
    }
}
