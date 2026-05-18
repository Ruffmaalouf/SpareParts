using Dapper;
using SpareParts.Domain.Scanning;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using System.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class ScanLookupService
{
    private readonly ISqlConnectionFactory _factory;

    public ScanLookupService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<ScanLookupResultDto> Resolve(string? scannedText)
    {
        var code = NormalizeScannedText(scannedText);
        if (string.IsNullOrWhiteSpace(code))
        {
            return Array.Empty<ScanLookupResultDto>();
        }

        var (targetType, lookupCode) = ParseTarget(code);
        var lookupId = targetType == null ? null : TryParseId(lookupCode);
        using var conn = _factory.CreateConnection();
        var results = new List<ScanLookupResultDto>();

        if (targetType is null or ScanTargetTypes.Part)
        {
            results.AddRange(QueryParts(conn, lookupCode, lookupId));
        }

        if (targetType is null or ScanTargetTypes.Warehouse)
        {
            results.AddRange(QueryWarehouses(conn, lookupCode, lookupId));
        }

        if (targetType is null or ScanTargetTypes.UsedCar)
        {
            results.AddRange(QueryUsedCars(conn, lookupCode, lookupId));
        }

        if (targetType is null or ScanTargetTypes.StockMovement)
        {
            results.AddRange(QueryStockMovements(conn, lookupCode, lookupId));
        }

        if (targetType is null or ScanTargetTypes.SaleInvoice)
        {
            results.AddRange(QueryTransactions(
                conn,
                lookupCode,
                lookupId,
                TransactionTypeKeys.Sale,
                ScanTargetTypes.SaleInvoice,
                "Sale invoice",
                "api/sales"));
        }

        if (targetType is null or ScanTargetTypes.PurchaseInvoice)
        {
            results.AddRange(QueryTransactions(
                conn,
                lookupCode,
                lookupId,
                TransactionTypeKeys.Purchase,
                ScanTargetTypes.PurchaseInvoice,
                "Purchase",
                "api/purchases"));
        }

        if (targetType is null or ScanTargetTypes.UsedCarPurchase)
        {
            results.AddRange(QueryTransactions(
                conn,
                lookupCode,
                lookupId,
                TransactionTypeKeys.UsedCarPurchase,
                ScanTargetTypes.UsedCarPurchase,
                "Used-car purchase",
                "api/purchases/usedcars"));
        }

        return results
            .GroupBy(item => $"{item.TargetType}:{item.TargetId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public static string NormalizeScannedText(string? scannedText)
    {
        var value = scannedText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var queryCode = TryReadQueryValue(uri.Query, "scan")
                ?? TryReadQueryValue(uri.Query, "code")
                ?? TryReadQueryValue(uri.Query, "barcode");
            if (!string.IsNullOrWhiteSpace(queryCode))
            {
                return queryCode.Trim();
            }

            if (string.Equals(uri.Scheme, "spareparts", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.AbsolutePath.Trim('/');
                if (!string.IsNullOrWhiteSpace(uri.Host) && !string.IsNullOrWhiteSpace(path))
                {
                    return $"{uri.Host}:{path}";
                }
            }
        }

        return value;
    }

    private static (string? TargetType, string Code) ParseTarget(string code)
    {
        var value = code.Trim();
        if (value.StartsWith("SPAREPARTS:", StringComparison.OrdinalIgnoreCase))
        {
            value = value["SPAREPARTS:".Length..].Trim();
        }

        var separatorIndex = value.IndexOfAny([':', '|']);
        if (separatorIndex <= 0 || separatorIndex >= value.Length - 1)
        {
            return (null, value);
        }

        var targetType = NormalizeTargetType(value[..separatorIndex]);
        if (targetType == null)
        {
            return (null, value);
        }

        return (targetType, value[(separatorIndex + 1)..].Trim());
    }

    private static string? NormalizeTargetType(string value)
    {
        var normalized = new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return normalized switch
        {
            "part" or "parts" or "p" => ScanTargetTypes.Part,
            "warehouse" or "warehouses" or "wh" => ScanTargetTypes.Warehouse,
            "usedcar" or "usedcars" or "uc" or "vehicle" => ScanTargetTypes.UsedCar,
            "stockmovement" or "stockmovements" or "movement" or "movements" or "sm" => ScanTargetTypes.StockMovement,
            "sale" or "sales" or "invoice" or "salesinvoice" or "salesinvoices" or "inv" => ScanTargetTypes.SaleInvoice,
            "purchase" or "purchases" or "purchaseinvoice" or "purchaseinvoices" or "pur" => ScanTargetTypes.PurchaseInvoice,
            "usedcarpurchase" or "usedcarpurchases" or "ucp" => ScanTargetTypes.UsedCarPurchase,
            _ => null
        };
    }

    private static int? TryParseId(string value)
        => int.TryParse(value.Trim(), out var id) && id > 0 ? id : null;

    private static IReadOnlyList<ScanLookupResultDto> QueryParts(IDbConnection conn, string code, int? id)
        => conn.Query<ScanLookupResultDto>(
            """
            SELECT TOP (20)
                   @TargetType AS TargetType,
                   p.Id AS TargetId,
                   COALESCE(NULLIF(LTRIM(RTRIM(p.Barcode)), N''), p.InternalCode) AS Code,
                   CONCAT(p.InternalCode, N' - ', p.Name) AS DisplayText,
                   NULLIF(LTRIM(RTRIM(p.OEMNumber)), N'') AS SecondaryText,
                   CONCAT(N'api/parts?scan=', @Code) AS ApiRoute
            FROM dbo.Parts p
            WHERE p.IsActive = 1
              AND (
                    NULLIF(LTRIM(RTRIM(p.Barcode)), N'') = @Code
                    OR NULLIF(LTRIM(RTRIM(p.InternalCode)), N'') = @Code
                    OR (@Id IS NOT NULL AND p.Id = @Id)
                  )
            ORDER BY p.Name;
            """,
            new { TargetType = ScanTargetTypes.Part, Code = code, Id = id }).ToList();

    private static IReadOnlyList<ScanLookupResultDto> QueryWarehouses(IDbConnection conn, string code, int? id)
        => conn.Query<ScanLookupResultDto>(
            """
            SELECT TOP (20)
                   @TargetType AS TargetType,
                   w.Id AS TargetId,
                   COALESCE(NULLIF(LTRIM(RTRIM(w.Barcode)), N''), CONCAT(N'WH-', w.Id)) AS Code,
                   w.Name AS DisplayText,
                   w.Address AS SecondaryText,
                   CONCAT(N'api/warehouses?scan=', @Code) AS ApiRoute
            FROM dbo.Warehouses w
            WHERE NULLIF(LTRIM(RTRIM(w.Barcode)), N'') = @Code
               OR (@Id IS NOT NULL AND w.Id = @Id)
            ORDER BY w.IsMain DESC, w.Name;
            """,
            new { TargetType = ScanTargetTypes.Warehouse, Code = code, Id = id }).ToList();

    private static IReadOnlyList<ScanLookupResultDto> QueryUsedCars(IDbConnection conn, string code, int? id)
        => conn.Query<ScanLookupResultDto>(
            """
            SELECT TOP (20)
                   @TargetType AS TargetType,
                   uc.Id AS TargetId,
                   COALESCE(NULLIF(LTRIM(RTRIM(uc.Barcode)), N''), CONCAT(N'UC-', uc.Id)) AS Code,
                   CONCAT(
                       CASE
                           WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN cm.Name
                           ELSE cb.Name + N' ' + cm.Name
                       END,
                       N' ',
                       CONVERT(NVARCHAR(4), uc.ModelYear)
                   ) AS DisplayText,
                   COALESCE(NULLIF(LTRIM(RTRIM(loc.Name)), N''), uc.Location) AS SecondaryText,
                   CONCAT(N'api/usedcars?scan=', @Code) AS ApiRoute
            FROM dbo.UsedCars uc
            INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
            INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
            LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
            WHERE NULLIF(LTRIM(RTRIM(uc.Barcode)), N'') = @Code
               OR (@Id IS NOT NULL AND uc.Id = @Id)
            ORDER BY uc.Id DESC;
            """,
            new { TargetType = ScanTargetTypes.UsedCar, Code = code, Id = id }).ToList();

    private static IReadOnlyList<ScanLookupResultDto> QueryStockMovements(IDbConnection conn, string code, int? id)
        => conn.Query<ScanLookupResultDto>(
            """
            SELECT TOP (20)
                   @TargetType AS TargetType,
                   sm.Id AS TargetId,
                   COALESCE(NULLIF(LTRIM(RTRIM(sm.ScanCode)), N''), CONCAT(N'SM-', sm.Id)) AS Code,
                   CONCAT(N'Movement ', COALESCE(NULLIF(LTRIM(RTRIM(sm.ScanCode)), N''), CONCAT(N'SM-', sm.Id)), N' - ', ISNULL(p.InternalCode, N'')) AS DisplayText,
                   CONCAT(ISNULL(w.Name, N''), N' | Qty ', CONVERT(NVARCHAR(40), sm.Quantity), N' | ', sm.MovementType) AS SecondaryText,
                   CONCAT(N'api/stockmovements?scan=', @Code) AS ApiRoute
            FROM dbo.StockMovements sm
            LEFT JOIN dbo.Parts p ON p.Id = sm.PartId
            LEFT JOIN dbo.Warehouses w ON w.Id = sm.WarehouseId
            WHERE NULLIF(LTRIM(RTRIM(sm.ScanCode)), N'') = @Code
               OR (@Id IS NOT NULL AND sm.Id = @Id)
            ORDER BY sm.Id DESC;
            """,
            new { TargetType = ScanTargetTypes.StockMovement, Code = code, Id = id }).ToList();

    private static IReadOnlyList<ScanLookupResultDto> QueryTransactions(
        IDbConnection conn,
        string code,
        int? id,
        string typeKey,
        string targetType,
        string displayPrefix,
        string apiRoute)
        => conn.Query<ScanLookupResultDto>(
            """
            SELECT TOP (20)
                   @TargetType AS TargetType,
                   t.ReferenceId AS TargetId,
                   COALESCE(NULLIF(LTRIM(RTRIM(t.ScanCode)), N''), t.TransactionNumber) AS Code,
                   CONCAT(@DisplayPrefix, N' ', t.TransactionNumber) AS DisplayText,
                   CONCAT(CONVERT(NVARCHAR(10), t.TransactionDate, 120), N' | ', CONVERT(NVARCHAR(40), t.TotalAmount), N' | ', t.PaymentStatus) AS SecondaryText,
                   CONCAT(@ApiRoute, N'?scan=', @Code) AS ApiRoute
            FROM dbo.Transactions t
            INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
            WHERE tt.TypeKey = @TypeKey
              AND (
                    NULLIF(LTRIM(RTRIM(t.ScanCode)), N'') = @Code
                    OR NULLIF(LTRIM(RTRIM(t.TransactionNumber)), N'') = @Code
                    OR (@Id IS NOT NULL AND t.ReferenceId = @Id)
                  )
            ORDER BY t.TransactionDate DESC, t.ReferenceId DESC;
            """,
            new
            {
                TargetType = targetType,
                Code = code,
                Id = id,
                TypeKey = typeKey,
                DisplayPrefix = displayPrefix,
                ApiRoute = apiRoute
            }).ToList();

    private static string? TryReadQueryValue(string query, string name)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var trimmed = query.TrimStart('?');
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 0 || !string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return parts.Length == 1 ? string.Empty : Uri.UnescapeDataString(parts[1]);
        }

        return null;
    }
}
