using Dapper;
using SpareParts.Domain.Scanning;
using SpareParts.Domain.Search;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using System.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class SmartSearchService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ScanLookupService _scanLookupService;

    public SmartSearchService(ISqlConnectionFactory factory, ScanLookupService scanLookupService)
    {
        _factory = factory;
        _scanLookupService = scanLookupService;
    }

    public SmartSearchResponseDto Search(string? query, int limit = 30)
    {
        var searchText = Normalize(query);
        var take = Math.Clamp(limit, 8, 60);
        using var connection = _factory.CreateConnection();
        var results = new List<SmartSearchResultDto>();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            results.AddRange(_scanLookupService.Resolve(searchText).Select(ToScanResult));
        }

        results.AddRange(QueryParts(connection, searchText, take));
        results.AddRange(QueryPartners(connection, searchText, take));
        results.AddRange(QueryTransactions(connection, searchText, take));
        results.AddRange(QueryUsedCars(connection, searchText, take));

        return new SmartSearchResponseDto
        {
            Query = searchText,
            GeneratedAt = DateTime.UtcNow,
            Results = results
                .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.EntityType))
                .GroupBy(item => $"{item.EntityType}:{item.Id}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Score).First())
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Section)
                .ThenBy(item => item.Title)
                .Take(take)
                .ToList()
        };
    }

    private static string Normalize(string? query)
        => (query ?? string.Empty).Trim();

    private static object Parameters(string searchText, int take)
        => new
        {
            Take = take,
            Query = searchText,
            Like = $"%{searchText}%",
            Prefix = $"{searchText}%",
            HasQuery = !string.IsNullOrWhiteSpace(searchText)
        };

    private static SmartSearchResultDto ToScanResult(ScanLookupResultDto result)
        => new()
        {
            Section = "Scan match",
            EntityType = result.TargetType,
            Id = result.TargetId,
            Title = result.DisplayText,
            Subtitle = result.SecondaryText ?? result.Code,
            Badge = result.Code,
            ActionLabel = "Open",
            TargetView = result.TargetType switch
            {
                ScanTargetTypes.Part => "parts",
                ScanTargetTypes.UsedCar => "used-cars",
                ScanTargetTypes.SaleInvoice => "invoices",
                ScanTargetTypes.PurchaseInvoice => "purchase-parts",
                ScanTargetTypes.UsedCarPurchase => "used-car-purchases",
                _ => "management"
            },
            ApiRoute = result.ApiRoute ?? string.Empty,
            Score = 120
        };

    private static IReadOnlyList<SmartSearchResultDto> QueryParts(IDbConnection connection, string searchText, int take)
        => connection.Query<SmartSearchResultDto>(
            """
            SELECT TOP (@Take)
                   N'Inventory' AS Section,
                   N'part' AS EntityType,
                   p.Id,
                   p.Name AS Title,
                   CONCAT(p.InternalCode, N' | ', ISNULL(NULLIF(p.OEMNumber, N''), N'No OEM'), N' | ', ISNULL(b.Name, N'No brand')) AS Subtitle,
                   CASE
                       WHEN ISNULL(stock.AvailableStock, 0) <= p.MinStock THEN CONCAT(N'Low stock ', ISNULL(stock.AvailableStock, 0))
                       ELSE CONCAT(N'Stock ', ISNULL(stock.AvailableStock, 0))
                   END AS Badge,
                   N'Open parts' AS ActionLabel,
                   N'parts' AS TargetView,
                   CONCAT(N'/api/parts?scan=', COALESCE(NULLIF(p.Barcode, N''), p.InternalCode, CONVERT(NVARCHAR(20), p.Id))) AS ApiRoute,
                   CASE
                       WHEN @HasQuery = 1 AND (p.Barcode = @Query OR p.InternalCode = @Query OR p.OEMNumber = @Query) THEN 110
                       WHEN @HasQuery = 1 AND (p.InternalCode LIKE @Prefix OR p.Name LIKE @Prefix OR p.OEMNumber LIKE @Prefix) THEN 92
                       WHEN @HasQuery = 1 THEN 72
                       WHEN ISNULL(stock.AvailableStock, 0) <= p.MinStock THEN 64
                       ELSE 44
                   END AS Score
            FROM dbo.Parts p
            LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
            LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
            OUTER APPLY
            (
                SELECT SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)) AS AvailableStock
                FROM dbo.Stock s
                WHERE s.PartId = p.Id
            ) stock
            WHERE p.IsActive = 1
              AND (
                    @HasQuery = 0
                    OR p.InternalCode LIKE @Like
                    OR p.Barcode LIKE @Like
                    OR p.Name LIKE @Like
                    OR p.OEMNumber LIKE @Like
                    OR b.Name LIKE @Like
                    OR c.Name LIKE @Like
                  )
            ORDER BY Score DESC, p.Name;
            """,
            Parameters(searchText, take)).ToList();

    private static IReadOnlyList<SmartSearchResultDto> QueryPartners(IDbConnection connection, string searchText, int take)
        => connection.Query<SmartSearchResultDto>(
            """
            SELECT TOP (@Take)
                   N'People' AS Section,
                   N'customer' AS EntityType,
                   c.Id,
                   c.Name AS Title,
                   CONCAT(ISNULL(NULLIF(c.Phone, N''), N'No phone'), N' | ', ISNULL(NULLIF(c.Email, N''), N'No email')) AS Subtitle,
                   N'Customer' AS Badge,
                   N'Open contacts' AS ActionLabel,
                   N'contacts' AS TargetView,
                   CONCAT(N'/api/customers?search=', @Query) AS ApiRoute,
                   CASE
                       WHEN @HasQuery = 1 AND (c.Phone = @Query OR c.Email = @Query OR c.TaxNumber = @Query) THEN 105
                       WHEN @HasQuery = 1 AND c.Name LIKE @Prefix THEN 88
                       WHEN @HasQuery = 1 THEN 68
                       ELSE 42
                   END AS Score
            FROM dbo.Customers c
            WHERE @HasQuery = 0
               OR c.Name LIKE @Like
               OR c.Phone LIKE @Like
               OR c.Email LIKE @Like
               OR c.TaxNumber LIKE @Like
            UNION ALL
            SELECT TOP (@Take)
                   N'People' AS Section,
                   N'supplier' AS EntityType,
                   s.Id,
                   s.Name AS Title,
                   CONCAT(ISNULL(NULLIF(s.Phone, N''), N'No phone'), N' | ', ISNULL(NULLIF(s.Email, N''), N'No email')) AS Subtitle,
                   N'Supplier' AS Badge,
                   N'Open contacts' AS ActionLabel,
                   N'contacts' AS TargetView,
                   CONCAT(N'/api/suppliers?search=', @Query) AS ApiRoute,
                   CASE
                       WHEN @HasQuery = 1 AND (s.Phone = @Query OR s.Email = @Query OR s.TaxNumber = @Query) THEN 104
                       WHEN @HasQuery = 1 AND s.Name LIKE @Prefix THEN 86
                       WHEN @HasQuery = 1 THEN 66
                       ELSE 40
                   END AS Score
            FROM dbo.Suppliers s
            WHERE @HasQuery = 0
               OR s.Name LIKE @Like
               OR s.Phone LIKE @Like
               OR s.Email LIKE @Like
               OR s.TaxNumber LIKE @Like;
            """,
            Parameters(searchText, take)).ToList();

    private static IReadOnlyList<SmartSearchResultDto> QueryTransactions(IDbConnection connection, string searchText, int take)
        => connection.Query<SmartSearchResultDto>(
            """
            SELECT TOP (@Take)
                   N'Documents' AS Section,
                   CASE
                       WHEN tt.TypeKey = @SaleType THEN N'sale'
                       WHEN tt.TypeKey = @PurchaseType THEN N'purchase'
                       WHEN tt.TypeKey = @UsedCarPurchaseType THEN N'used-car-purchase'
                       ELSE N'transaction'
                   END AS EntityType,
                   t.ReferenceId AS Id,
                   t.TransactionNumber AS Title,
                   CONCAT(
                       CONVERT(NVARCHAR(10), t.TransactionDate, 120),
                       N' | ',
                       COALESCE(NULLIF(c.Name, N''), NULLIF(s.Name, N''), N'No party'),
                       N' | ',
                       CONVERT(NVARCHAR(32), t.TotalAmount)
                   ) AS Subtitle,
                   t.PaymentStatus AS Badge,
                   N'Open document' AS ActionLabel,
                   CASE
                       WHEN tt.TypeKey = @SaleType THEN N'invoices'
                       WHEN tt.TypeKey = @PurchaseType THEN N'purchase-parts'
                       WHEN tt.TypeKey = @UsedCarPurchaseType THEN N'used-car-purchases'
                       ELSE N'invoices'
                   END AS TargetView,
                   CONCAT(N'/api/transactions?scan=', COALESCE(NULLIF(t.ScanCode, N''), t.TransactionNumber)) AS ApiRoute,
                   CASE
                       WHEN @HasQuery = 1 AND (t.TransactionNumber = @Query OR t.ScanCode = @Query) THEN 108
                       WHEN @HasQuery = 1 AND (t.TransactionNumber LIKE @Prefix OR t.ScanCode LIKE @Prefix) THEN 90
                       WHEN @HasQuery = 1 THEN 70
                       ELSE 46
                   END AS Score
            FROM dbo.Transactions t
            INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
            LEFT JOIN dbo.Customers c ON c.Id = t.CustomerId
            LEFT JOIN dbo.Suppliers s ON s.Id = t.SupplierId
            WHERE tt.TypeKey IN (@SaleType, @PurchaseType, @UsedCarPurchaseType)
              AND (
                    @HasQuery = 0
                    OR t.TransactionNumber LIKE @Like
                    OR t.ScanCode LIKE @Like
                    OR c.Name LIKE @Like
                    OR s.Name LIKE @Like
                    OR CAST(t.ReferenceId AS NVARCHAR(50)) LIKE @Like
                  )
            ORDER BY Score DESC, t.TransactionDate DESC, t.ReferenceId DESC;
            """,
            new
            {
                Parameters = Parameters(searchText, take),
                Take = take,
                Query = searchText,
                Like = $"%{searchText}%",
                Prefix = $"{searchText}%",
                HasQuery = !string.IsNullOrWhiteSpace(searchText),
                SaleType = TransactionTypeKeys.Sale,
                PurchaseType = TransactionTypeKeys.Purchase,
                UsedCarPurchaseType = TransactionTypeKeys.UsedCarPurchase
            }).ToList();

    private static IReadOnlyList<SmartSearchResultDto> QueryUsedCars(IDbConnection connection, string searchText, int take)
        => connection.Query<SmartSearchResultDto>(
            """
            SELECT TOP (@Take)
                   N'Used cars' AS Section,
                   N'used-car' AS EntityType,
                   uc.Id,
                   CONCAT(ISNULL(cb.Name, N''), N' ', ISNULL(cm.Name, N''), N' ', CONVERT(NVARCHAR(4), uc.ModelYear)) AS Title,
                   CONCAT(COALESCE(NULLIF(uc.Barcode, N''), CONCAT(N'UC-', uc.Id)), N' | ', COALESCE(NULLIF(loc.Name, N''), NULLIF(uc.Location, N''), N'No location')) AS Subtitle,
                   CASE WHEN uc.IsReceived = 1 THEN N'Received' ELSE N'In transit' END AS Badge,
                   N'Open used cars' AS ActionLabel,
                   N'used-cars' AS TargetView,
                   CONCAT(N'/api/usedcars?scan=', COALESCE(NULLIF(uc.Barcode, N''), CONVERT(NVARCHAR(20), uc.Id))) AS ApiRoute,
                   CASE
                       WHEN @HasQuery = 1 AND uc.Barcode = @Query THEN 106
                       WHEN @HasQuery = 1 AND (cb.Name LIKE @Prefix OR cm.Name LIKE @Prefix) THEN 84
                       WHEN @HasQuery = 1 THEN 62
                       WHEN uc.IsReceived = 1 THEN 45
                       ELSE 38
                   END AS Score
            FROM dbo.UsedCars uc
            INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
            INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
            LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
            WHERE @HasQuery = 0
               OR uc.Barcode LIKE @Like
               OR cb.Name LIKE @Like
               OR cm.Name LIKE @Like
               OR uc.Location LIKE @Like
               OR loc.Name LIKE @Like
               OR CONVERT(NVARCHAR(4), uc.ModelYear) LIKE @Like
               OR CAST(uc.Id AS NVARCHAR(50)) LIKE @Like
            ORDER BY Score DESC, uc.Id DESC;
            """,
            Parameters(searchText, take)).ToList();
}
