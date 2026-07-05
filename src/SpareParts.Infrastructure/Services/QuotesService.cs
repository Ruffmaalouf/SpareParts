using Dapper;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class QuotesService
{
    private const string QuoteNumberPrefix = "QT";

    private readonly ISqlConnectionFactory _factory;
    private readonly SalesService _salesService;
    private readonly ITenantContext _tenantContext;

    public QuotesService(ISqlConnectionFactory factory, SalesService salesService, ITenantContext tenantContext)
    {
        _factory = factory;
        _salesService = salesService;
        _tenantContext = tenantContext;
    }

    public IEnumerable<QuoteLookupDto> GetAll(string? status = null, string? search = null)
    {
        using var conn = _factory.CreateConnection();
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        return conn.Query<QuoteLookupDto>(
            """
SELECT
    q.Id,
    q.QuoteNumber,
    q.QuoteDate,
    q.ExpiryDate,
    q.CustomerId,
    q.CustomerName,
    q.CustomerPhone,
    q.Status,
    ISNULL(
        (SELECT SUM(qi.Quantity * qi.UnitPrice - qi.DiscountAmount)
         FROM dbo.QuoteItems qi
         WHERE qi.QuoteId = q.Id), 0) AS TotalAmount
FROM dbo.Quotes q
LEFT JOIN dbo.Warehouses w ON w.Id = q.WarehouseId
WHERE (@TenantId = 0 OR q.WarehouseId IS NULL OR w.TenantId = @TenantId)
  AND (@Status IS NULL OR q.Status = @Status)
  AND (
      @Search IS NULL
      OR q.QuoteNumber     LIKE N'%' + @Search + N'%'
      OR q.CustomerName    LIKE N'%' + @Search + N'%'
      OR ISNULL(q.CustomerPhone, N'') LIKE N'%' + @Search + N'%'
  )
ORDER BY q.QuoteDate DESC, q.Id DESC;
""",
            new { TenantId = _tenantContext.TenantId, Status = normalizedStatus, Search = normalizedSearch });
    }

    public QuoteDetailsDto? GetById(int id)
    {
        using var conn = _factory.CreateConnection();

        var quote = conn.QuerySingleOrDefault<QuoteDetailsDto>(
            """
SELECT
    q.Id,
    q.QuoteNumber,
    q.QuoteDate,
    q.ExpiryDate,
    q.CustomerId,
    q.CustomerName,
    q.CustomerPhone,
    q.WarehouseId,
    q.Status,
    q.Notes,
    q.CreatedAt
FROM dbo.Quotes q
LEFT JOIN dbo.Warehouses w ON w.Id = q.WarehouseId
WHERE q.Id = @Id
  AND (@TenantId = 0 OR q.WarehouseId IS NULL OR w.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId });

        if (quote == null) return null;

        quote.Items = conn.Query<QuoteLineDto>(
            """
SELECT
    qi.Id,
    qi.PartId,
    ISNULL(p.Name, qi.Description) AS Description,
    qi.Quantity,
    qi.UnitPrice,
    qi.DiscountAmount,
    qi.SortOrder
FROM dbo.QuoteItems qi
LEFT JOIN dbo.Parts p ON p.Id = qi.PartId
WHERE qi.QuoteId = @QuoteId
ORDER BY qi.SortOrder, qi.Id;
""",
            new { QuoteId = id }).ToList();

        return quote;
    }

    public int Create(CreateQuoteRequest request, int userId)
    {
        using var conn = _factory.CreateConnection();

        if (request.WarehouseId.HasValue)
        {
            var warehouseOk = conn.ExecuteScalar<bool>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Warehouses WHERE Id = @WarehouseId AND (@TenantId = 0 OR TenantId = @TenantId)) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;",
                new { request.WarehouseId, TenantId = _tenantContext.TenantId });

            if (!warehouseOk)
                throw new NotFoundException($"Warehouse {request.WarehouseId} not found.");
        }

        var quoteNumber = GenerateQuoteNumber(conn);

        var quoteId = conn.ExecuteScalar<int>(
            """
INSERT INTO dbo.Quotes
    (QuoteNumber, QuoteDate, ExpiryDate, CustomerId, CustomerName, CustomerPhone, WarehouseId, Status, Notes, CreatedAt, CreatedByUserId, TenantId)
VALUES
    (@QuoteNumber, @QuoteDate, @ExpiryDate, @CustomerId, @CustomerName, @CustomerPhone, @WarehouseId, N'Draft', @Notes, SYSUTCDATETIME(), @UserId, @TenantId);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                QuoteNumber = quoteNumber,
                QuoteDate = request.QuoteDate == default ? DateTime.UtcNow : request.QuoteDate,
                request.ExpiryDate,
                request.CustomerId,
                CustomerName = (request.CustomerName ?? string.Empty).Trim(),
                CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim(),
                request.WarehouseId,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                UserId = userId,
                // Stamp the creating tenant on insert so new quotes are correctly scoped immediately
                // (SEC-1). Without this the row is TenantId=NULL until TenantIdMigration backfills it to
                // the default tenant — mis-assigning quotes on a multi-tenant DB and hiding them from the
                // owning tenant's Ignition Timeline/Quotes tabs. Mirrors CreateSaleHandler's tenant stamping.
                TenantId = _tenantContext.TenantId
            });

        for (var i = 0; i < request.Items.Count; i++)
        {
            var line = request.Items[i];
            conn.Execute(
                """
INSERT INTO dbo.QuoteItems (QuoteId, PartId, Description, Quantity, UnitPrice, DiscountAmount, SortOrder)
VALUES (@QuoteId, @PartId, @Description, @Quantity, @UnitPrice, @DiscountAmount, @SortOrder);
""",
                new
                {
                    QuoteId = quoteId,
                    line.PartId,
                    Description = (line.Description ?? string.Empty).Trim(),
                    Quantity = Math.Max(1, line.Quantity),
                    line.UnitPrice,
                    line.DiscountAmount,
                    SortOrder = i + 1
                });
        }

        return quoteId;
    }

    public void UpdateStatus(int id, string status)
    {
        using var conn = _factory.CreateConnection();
        conn.Execute(
            """
UPDATE q SET q.Status = @Status
FROM dbo.Quotes q
LEFT JOIN dbo.Warehouses w ON w.Id = q.WarehouseId
WHERE q.Id = @Id
  AND (@TenantId = 0 OR q.WarehouseId IS NULL OR w.TenantId = @TenantId);
""",
            new { Id = id, Status = status.Trim(), TenantId = _tenantContext.TenantId });
    }

    public ConvertQuoteToInvoiceResponse ConvertToInvoice(int quoteId, int userId)
    {
        var quote = GetById(quoteId) ?? throw new NotFoundException($"Quote {quoteId} not found.");

        if (quote.Items.Count == 0)
            throw new ValidationException("Cannot convert a quote with no line items to an invoice.");

        var catalogItems = quote.Items.Where(item => item.PartId.HasValue && item.PartId.Value > 0).ToList();
        if (catalogItems.Count == 0)
            throw new ValidationException("At least one quote line must be matched to a catalog part before converting to an invoice.");

        if (quote.WarehouseId == null)
            throw new ValidationException("Quote must have a warehouse selected before converting to an invoice.");

        var saleRequest = new CreateSaleRequest
        {
            InvoiceDate = quote.QuoteDate,
            CustomerId = quote.CustomerId,
            WarehouseId = quote.WarehouseId.Value,
            PaymentMethod = "Cash",
            PaidAmount = 0m,
            Notes = $"Converted from quote {quote.QuoteNumber}. {quote.Notes}".Trim(),
            Items = quote.Items
                .Where(item => item.PartId.HasValue && item.PartId.Value > 0)
                .Select(item => new SaleItemDto
                {
                    PartId = item.PartId!.Value,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TaxRate = 0m
                }).ToList()
        };

        var response = _salesService.CreateSale(saleRequest, userId);

        UpdateStatus(quoteId, "Converted");

        return new ConvertQuoteToInvoiceResponse
        {
            InvoiceId = response.InvoiceId,
            InvoiceNumber = response.InvoiceNumber
        };
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var ownedByTenant = conn.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.Quotes q
    LEFT JOIN dbo.Warehouses w ON w.Id = q.WarehouseId
    WHERE q.Id = @Id AND (@TenantId = 0 OR q.WarehouseId IS NULL OR w.TenantId = @TenantId)
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { Id = id, TenantId = _tenantContext.TenantId });

        if (!ownedByTenant)
            throw new NotFoundException($"Quote {id} not found.");

        conn.Execute("DELETE FROM dbo.QuoteItems WHERE QuoteId = @Id;", new { Id = id });
        conn.Execute("DELETE FROM dbo.Quotes WHERE Id = @Id;", new { Id = id });
    }

    private static string GenerateQuoteNumber(System.Data.IDbConnection conn)
    {
        var lastNumber = conn.ExecuteScalar<int?>(
            "SELECT TOP 1 TRY_CAST(SUBSTRING(QuoteNumber, 3, 20) AS INT) FROM dbo.Quotes ORDER BY Id DESC;");
        var next = (lastNumber ?? 0) + 1;
        return $"{QuoteNumberPrefix}{next:D6}";
    }
}
