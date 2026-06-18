using Dapper;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Reports;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;
using System.Data;
using System.Globalization;

namespace SpareParts.Infrastructure.Services;

public sealed partial class ReportBuilderService
{
    private static readonly HashSet<string> RestrictedTableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Users",
        "Roles",
        "RoleMenuAccess",
        "AppMenus",
        "AppConstants",
        "ApplicationExceptionLogs",
        "__EFMigrationsHistory"
    };

    private static readonly HashSet<string> UnsupportedSqlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "timestamp",
        "rowversion",
        "image",
        "binary",
        "varbinary",
        "xml",
        "geography",
        "geometry",
        "hierarchyid"
    };

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ReportBuilderService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<ReportTableDto> GetTables()
    {
        using var connection = _factory.CreateConnection();

        var tables = connection.Query<ReportTableDto>(
                @"
SELECT CONCAT(s.name, '.', t.name) AS [Key],
       s.name AS SchemaName,
       t.name AS TableName,
       COUNT(c.column_id) AS ColumnCount
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0
  AND s.name NOT IN ('sys', 'INFORMATION_SCHEMA')
  AND ty.name NOT IN @UnsupportedSqlTypes
  AND LOWER(c.name) NOT LIKE '%password%'
  AND LOWER(c.name) NOT LIKE '%token%'
  AND LOWER(c.name) NOT LIKE '%secret%'
  AND LOWER(c.name) NOT LIKE '%apikey%'
  AND LOWER(c.name) NOT LIKE '%api_key%'
GROUP BY s.name, t.name
HAVING COUNT(c.column_id) > 0
ORDER BY s.name, t.name;",
                new { UnsupportedSqlTypes })
            .Where(table => !IsRestrictedTable(table.SchemaName, table.TableName))
            .ToList();

        foreach (var table in tables)
        {
            PrepareTable(table);
        }

        return tables;
    }

    public IReadOnlyList<ReportColumnDto> GetColumns(string tableKey)
    {
        using var connection = _factory.CreateConnection();
        var table = GetTable(connection, tableKey)
            ?? throw new NotFoundException($"System table '{tableKey}' was not found.");

        return GetColumns(connection, table);
    }

    public IReadOnlyList<ReportTableLinkDto> GetLinks()
    {
        using var connection = _factory.CreateConnection();
        return GetLinks(connection);
    }

    public ReportTableLinkDto SaveLink(SaveReportTableLinkRequest request, int currentRoleId)
    {
        EnsureAdmin(currentRoleId);

        if (request == null)
        {
            throw new ValidationException("Table link payload is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceTableKey) || string.IsNullOrWhiteSpace(request.TargetTableKey))
        {
            throw new ValidationException("Choose both source and target tables.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceColumnName) || string.IsNullOrWhiteSpace(request.TargetColumnName))
        {
            throw new ValidationException("Choose both source and target columns.");
        }

        var joinType = NormalizeJoinType(request.JoinType);

        using var connection = _factory.CreateConnection();
        var sourceTable = GetTable(connection, request.SourceTableKey)
            ?? throw new NotFoundException($"Source table '{request.SourceTableKey}' was not found.");
        var targetTable = GetTable(connection, request.TargetTableKey)
            ?? throw new NotFoundException($"Target table '{request.TargetTableKey}' was not found.");

        if (string.Equals(sourceTable.Key, targetTable.Key, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Choose two different tables for a custom link.");
        }

        var sourceColumn = GetColumns(connection, sourceTable)
            .FirstOrDefault(column => string.Equals(column.ColumnName, request.SourceColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException($"Column '{request.SourceColumnName}' was not found on {sourceTable.Key}.");

        var targetColumn = GetColumns(connection, targetTable)
            .FirstOrDefault(column => string.Equals(column.ColumnName, request.TargetColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException($"Column '{request.TargetColumnName}' was not found on {targetTable.Key}.");

        var linkName = string.IsNullOrWhiteSpace(request.LinkName)
            ? $"{sourceTable.DisplayName} {sourceColumn.DisplayName} to {targetTable.DisplayName} {targetColumn.DisplayName}"
            : request.LinkName.Trim();

        var duplicateCount = connection.ExecuteScalar<int>(
            @"
SELECT COUNT(1)
FROM dbo.ReportBuilderTableLinks
WHERE SourceTableKey = @SourceTableKey
  AND SourceColumnName = @SourceColumnName
  AND TargetTableKey = @TargetTableKey
  AND TargetColumnName = @TargetColumnName
  AND (@Id IS NULL OR Id <> @Id)
  AND (@TenantId = 0 OR TenantId = @TenantId);",
            new
            {
                request.Id,
                SourceTableKey = sourceTable.Key,
                SourceColumnName = sourceColumn.ColumnName,
                TargetTableKey = targetTable.Key,
                TargetColumnName = targetColumn.ColumnName,
                _tenantContext.TenantId
            });

        if (duplicateCount > 0)
        {
            throw new ConflictException("This table link already exists.");
        }

        int linkId;
        if (request.Id is > 0)
        {
            var updated = connection.Execute(
                @"
UPDATE dbo.ReportBuilderTableLinks
SET LinkName = @LinkName,
    SourceTableKey = @SourceTableKey,
    SourceColumnName = @SourceColumnName,
    TargetTableKey = @TargetTableKey,
    TargetColumnName = @TargetColumnName,
    JoinType = @JoinType,
    IsActive = 1,
    ModifiedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND (@TenantId = 0 OR TenantId = @TenantId);",
                new
                {
                    Id = request.Id.Value,
                    LinkName = linkName,
                    SourceTableKey = sourceTable.Key,
                    SourceColumnName = sourceColumn.ColumnName,
                    TargetTableKey = targetTable.Key,
                    TargetColumnName = targetColumn.ColumnName,
                    JoinType = joinType,
                    _tenantContext.TenantId
                });

            if (updated == 0)
            {
                throw new NotFoundException($"Report table link '{request.Id}' was not found.");
            }

            linkId = request.Id.Value;
        }
        else
        {
            var tenantId = _tenantContext.TenantId > 0 ? (int?)_tenantContext.TenantId : null;
            linkId = connection.ExecuteScalar<int>(
                @"
INSERT INTO dbo.ReportBuilderTableLinks
(
    LinkName,
    SourceTableKey,
    SourceColumnName,
    TargetTableKey,
    TargetColumnName,
    JoinType,
    IsActive,
    TenantId
)
VALUES
(
    @LinkName,
    @SourceTableKey,
    @SourceColumnName,
    @TargetTableKey,
    @TargetColumnName,
    @JoinType,
    1,
    @TenantId
);

SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    LinkName = linkName,
                    SourceTableKey = sourceTable.Key,
                    SourceColumnName = sourceColumn.ColumnName,
                    TargetTableKey = targetTable.Key,
                    TargetColumnName = targetColumn.ColumnName,
                    JoinType = joinType,
                    TenantId = tenantId
                });
        }

        return GetLinks(connection)
            .First(link => link.Id == linkId);
    }

    public void DeleteLink(int id, int currentRoleId)
    {
        EnsureAdmin(currentRoleId);

        using var connection = _factory.CreateConnection();
        var deleted = connection.Execute(
            "DELETE FROM dbo.ReportBuilderTableLinks WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, _tenantContext.TenantId });
        if (deleted == 0)
        {
            throw new NotFoundException($"Report table link '{id}' was not found.");
        }
    }

    public ReportResultDto RunReport(RunReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableKey))
        {
            throw new ValidationException("Choose a table before running the report.");
        }

        using var connection = _factory.CreateConnection();
        var rootTable = GetTable(connection, request.TableKey)
            ?? throw new NotFoundException($"System table '{request.TableKey}' was not found.");

        var selectedLinks = ResolveRequestedLinks(connection, request.Joins);
        var aliasByTableKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [rootTable.Key] = "t0"
        };
        var activeTables = new Dictionary<string, ReportTableDto>(StringComparer.OrdinalIgnoreCase)
        {
            [rootTable.Key] = rootTable
        };
        var activeTableOrder = new List<string> { rootTable.Key };
        var joinSqlParts = new List<string>();

        foreach (var link in selectedLinks)
        {
            ApplyJoin(connection, link, aliasByTableKey, activeTables, activeTableOrder, joinSqlParts);
        }

        var availableColumns = new List<ReportColumnDto>();
        foreach (var tableKey in activeTableOrder)
        {
            availableColumns.AddRange(GetColumns(connection, activeTables[tableKey]));
        }

        if (availableColumns.Count == 0)
        {
            throw new ValidationException($"System table '{rootTable.Key}' does not have reportable columns.");
        }

        var columnsByKey = availableColumns.ToDictionary(column => column.Key, StringComparer.OrdinalIgnoreCase);
        var columnsByQualifiedName = availableColumns.ToDictionary(column => column.QualifiedColumnName, StringComparer.OrdinalIgnoreCase);
        var uniqueColumnsByName = availableColumns
            .GroupBy(column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var uniqueColumnsByDisplayName = availableColumns
            .GroupBy(column => column.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var maxRows = Math.Clamp(request.MaxRows <= 0 ? 500 : request.MaxRows, 1, 5000);
        var parameters = new DynamicParameters();
        parameters.Add("MaxRows", maxRows);

        var whereClauses = new List<string>();
        var parameterIndex = 0;
        foreach (var filter in request.Filters ?? Enumerable.Empty<ReportFilterRequest>())
        {
            var clause = BuildFilterClause(filter, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, aliasByTableKey, parameters, ref parameterIndex);
            if (!string.IsNullOrWhiteSpace(clause))
            {
                whereClauses.Add(clause);
            }
        }

        var whereSql = whereClauses.Count == 0 ? string.Empty : $" WHERE {string.Join(" AND ", whereClauses)}";
        var fromSql = $@"
FROM {Quote(rootTable.SchemaName)}.{Quote(rootTable.TableName)} t0
{string.Join(Environment.NewLine, joinSqlParts)}
{whereSql}";

        var baseBindings = BuildFormulaBindings(availableColumns, aliasByTableKey, uniqueColumnsByName, uniqueColumnsByDisplayName);
        var calculatedColumns = ResolveCalculatedColumns(request.CalculatedColumns, baseBindings);
        var groupColumns = ResolveGroupColumns(request.GroupByColumns, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, uniqueColumnsByDisplayName, aliasByTableKey);
        var aggregateColumns = ResolveAggregateColumns(request.Aggregates, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, uniqueColumnsByDisplayName, aliasByTableKey);
        var currencyProjection = ResolveCurrencyProjectionContext(connection, request.CurrencySettings, availableColumns, aliasByTableKey);
        var isGrouped = groupColumns.Count > 0 || aggregateColumns.Count > 0;

        return isGrouped
            ? RunGroupedReport(connection, request, rootTable, maxRows, parameters, fromSql, availableColumns, groupColumns, aggregateColumns, currencyProjection)
            : RunDetailedReport(connection, request, rootTable, maxRows, parameters, fromSql, availableColumns, calculatedColumns, aliasByTableKey, currencyProjection);
    }

    private ReportResultDto RunDetailedReport(
        IDbConnection connection,
        RunReportRequest request,
        ReportTableDto rootTable,
        int maxRows,
        DynamicParameters parameters,
        string fromSql,
        IReadOnlyList<ReportColumnDto> availableColumns,
        IReadOnlyList<(string Alias, string SqlExpression)> calculatedColumns,
        IReadOnlyDictionary<string, string> aliasByTableKey,
        ReportCurrencyProjectionContext currencyProjection)
    {
        var selectedColumns = (request.Columns ?? new List<string>()).Any(value => !string.IsNullOrWhiteSpace(value))
            ? ResolveSelectedColumns(request, availableColumns, rootTable.Key)
            : calculatedColumns.Count > 0
                ? new List<ReportColumnDto>()
                : ResolveSelectedColumns(request, availableColumns, rootTable.Key);
        var projections = new List<(string OutputAlias, string SelectExpression, ReportResultColumnDto ResultColumn)>();
        var sortExpressions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var outputIndex = 0;

        foreach (var availableColumn in availableColumns)
        {
            AddSortIdentifiers(sortExpressions, availableColumn, $"{aliasByTableKey[availableColumn.TableKey]}.{Quote(availableColumn.ColumnName)}");
        }

        foreach (var column in selectedColumns)
        {
            var rawExpression = $"{aliasByTableKey[column.TableKey]}.{Quote(column.ColumnName)}";

            var outputAlias = $"c{outputIndex++}";
            projections.Add((
                outputAlias,
                rawExpression,
                new ReportResultColumnDto
                {
                    Key = column.Key,
                    ColumnName = outputAlias,
                    DisplayName = column.QualifiedDisplayName,
                    DataType = column.DataType,
                    SourceType = "column"
                }));
        }

        foreach (var calculated in calculatedColumns)
        {
            var outputAlias = $"c{outputIndex++}";
            projections.Add((
                outputAlias,
                calculated.SqlExpression,
                new ReportResultColumnDto
                {
                    Key = calculated.Alias,
                    ColumnName = outputAlias,
                    DisplayName = calculated.Alias,
                    DataType = "number",
                    SourceType = "calculated"
                }));
            sortExpressions[calculated.Alias] = calculated.SqlExpression;
        }

        if (currencyProjection.IsEnabled)
        {
            projections.Add((
                $"c{outputIndex++}",
                currencyProjection.TransactionCurrencyExpression,
                new ReportResultColumnDto
                {
                    Key = "currency.transaction",
                    ColumnName = $"c{outputIndex - 1}",
                    DisplayName = "Transaction Currency",
                    DataType = "string",
                    SourceType = "currency"
                }));
            projections.Add((
                $"c{outputIndex++}",
                currencyProjection.BaseCurrencyCodeExpression,
                new ReportResultColumnDto
                {
                    Key = "currency.base.code",
                    ColumnName = $"c{outputIndex - 1}",
                    DisplayName = "Base Currency",
                    DataType = "string",
                    SourceType = "currency"
                }));
            projections.Add((
                $"c{outputIndex++}",
                currencyProjection.BaseAmountExpression,
                new ReportResultColumnDto
                {
                    Key = "currency.base.amount",
                    ColumnName = $"c{outputIndex - 1}",
                    DisplayName = "Base Amount",
                    DataType = "number",
                    SourceType = "currency"
                }));
            projections.Add((
                $"c{outputIndex++}",
                currencyProjection.CounterCurrencyCodeExpression,
                new ReportResultColumnDto
                {
                    Key = "currency.counter.code",
                    ColumnName = $"c{outputIndex - 1}",
                    DisplayName = "Counter Currency",
                    DataType = "string",
                    SourceType = "currency"
                }));
            projections.Add((
                $"c{outputIndex++}",
                currencyProjection.CounterAmountExpression,
                new ReportResultColumnDto
                {
                    Key = "currency.counter.amount",
                    ColumnName = $"c{outputIndex - 1}",
                    DisplayName = "Counter Amount",
                    DataType = "number",
                    SourceType = "currency"
                }));
            projections.Add((
                $"c{outputIndex++}",
                currencyProjection.ReportingAmountExpression,
                new ReportResultColumnDto
                {
                    Key = "currency.reporting.amount",
                    ColumnName = $"c{outputIndex - 1}",
                    DisplayName = $"Reporting Amount ({currencyProjection.ReportingCurrencyCode})",
                    DataType = "number",
                    SourceType = "currency"
                }));
        }

        var selectSql = string.Join(", ", projections.Select(item => $"{item.SelectExpression} AS [{item.OutputAlias}]"));
        var orderSql = BuildOutputOrderByClause(request.SortColumn, request.SortDescending, sortExpressions);
        var sql = $@"
SELECT TOP (@MaxRows) {selectSql}
{fromSql}
{orderSql};";

        var rows = connection.Query(sql, parameters)
            .Select(row =>
            {
                var values = (IDictionary<string, object?>)row;
                return values.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            })
            .ToList();

        return new ReportResultDto
        {
            TableName = rootTable.Key,
            Columns = projections.Select(item => item.ResultColumn).ToList(),
            Rows = rows,
            RowCount = rows.Count,
            MaxRows = maxRows,
            IsGrouped = false,
            GeneratedSql = request.IncludeSqlPreview ? sql : string.Empty
        };
    }

    private ReportResultDto RunGroupedReport(
        IDbConnection connection,
        RunReportRequest request,
        ReportTableDto rootTable,
        int maxRows,
        DynamicParameters parameters,
        string fromSql,
        IReadOnlyList<ReportColumnDto> availableColumns,
        IReadOnlyList<(ReportColumnDto Column, string SqlExpression)> groupColumns,
        IReadOnlyList<(string Alias, string AggregateType, string SqlExpression, string DataType)> aggregateColumns,
        ReportCurrencyProjectionContext currencyProjection)
    {
        var innerSelectParts = new List<string>();
        var outerSelectParts = new List<string>();
        var resultColumns = new List<ReportResultColumnDto>();
        var summaryBindings = new Dictionary<string, (string SqlExpression, bool SupportsArithmetic)>(StringComparer.OrdinalIgnoreCase);
        var sortExpressions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var groupByParts = new List<string>();
        var uniqueGroupDisplayNames = groupColumns
            .GroupBy(item => item.Column.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().Column, StringComparer.OrdinalIgnoreCase);
        var outputIndex = 0;

        for (var index = 0; index < groupColumns.Count; index++)
        {
            var groupColumn = groupColumns[index];
            var innerAlias = $"g{index}";
            var outputAlias = $"c{outputIndex++}";

            innerSelectParts.Add($"{groupColumn.SqlExpression} AS [{innerAlias}]");
            outerSelectParts.Add($"summary.[{innerAlias}] AS [{outputAlias}]");
            groupByParts.Add(groupColumn.SqlExpression);
            resultColumns.Add(new ReportResultColumnDto
            {
                Key = groupColumn.Column.Key,
                ColumnName = outputAlias,
                DisplayName = groupColumn.Column.QualifiedDisplayName,
                DataType = groupColumn.Column.DataType,
                SourceType = "group"
            });

            AddSortIdentifiers(sortExpressions, groupColumn.Column, $"summary.[{innerAlias}]");
            AddFormulaBindings(summaryBindings, groupColumn.Column, $"summary.[{innerAlias}]", uniqueGroupDisplayNames);
        }

        for (var index = 0; index < aggregateColumns.Count; index++)
        {
            var aggregate = aggregateColumns[index];
            var innerAlias = $"a{index}";
            var outputAlias = $"c{outputIndex++}";

            innerSelectParts.Add($"{aggregate.SqlExpression} AS [{innerAlias}]");
            outerSelectParts.Add($"summary.[{innerAlias}] AS [{outputAlias}]");
            resultColumns.Add(new ReportResultColumnDto
            {
                Key = aggregate.Alias,
                ColumnName = outputAlias,
                DisplayName = aggregate.Alias,
                DataType = aggregate.DataType,
                SourceType = "aggregate"
            });

            sortExpressions[aggregate.Alias] = $"summary.[{innerAlias}]";
            summaryBindings[ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(aggregate.Alias)] =
                ($"summary.[{innerAlias}]", string.Equals(aggregate.DataType, "number", StringComparison.OrdinalIgnoreCase));
        }

        if (currencyProjection.IsEnabled)
        {
            var baseInnerAlias = $"a{aggregateColumns.Count}";
            var baseOutputAlias = $"c{outputIndex++}";
            innerSelectParts.Add($"SUM({currencyProjection.BaseAmountExpression}) AS [{baseInnerAlias}]");
            outerSelectParts.Add($"summary.[{baseInnerAlias}] AS [{baseOutputAlias}]");
            resultColumns.Add(new ReportResultColumnDto
            {
                Key = "currency.base.amount",
                ColumnName = baseOutputAlias,
                DisplayName = "Base Amount",
                DataType = "number",
                SourceType = "currency"
            });
            sortExpressions["Base Amount"] = $"summary.[{baseInnerAlias}]";

            var counterInnerAlias = $"a{aggregateColumns.Count + 1}";
            var counterOutputAlias = $"c{outputIndex++}";
            innerSelectParts.Add($"SUM({currencyProjection.CounterAmountExpression}) AS [{counterInnerAlias}]");
            outerSelectParts.Add($"summary.[{counterInnerAlias}] AS [{counterOutputAlias}]");
            resultColumns.Add(new ReportResultColumnDto
            {
                Key = "currency.counter.amount",
                ColumnName = counterOutputAlias,
                DisplayName = "Counter Amount",
                DataType = "number",
                SourceType = "currency"
            });
            sortExpressions["Counter Amount"] = $"summary.[{counterInnerAlias}]";

            var reportingInnerAlias = $"a{aggregateColumns.Count + 2}";
            var reportingOutputAlias = $"c{outputIndex++}";
            innerSelectParts.Add($"SUM({currencyProjection.ReportingAmountExpression}) AS [{reportingInnerAlias}]");
            outerSelectParts.Add($"summary.[{reportingInnerAlias}] AS [{reportingOutputAlias}]");
            resultColumns.Add(new ReportResultColumnDto
            {
                Key = "currency.reporting.amount",
                ColumnName = reportingOutputAlias,
                DisplayName = $"Reporting Amount ({currencyProjection.ReportingCurrencyCode})",
                DataType = "number",
                SourceType = "currency"
            });
            sortExpressions[$"Reporting Amount ({currencyProjection.ReportingCurrencyCode})"] = $"summary.[{reportingInnerAlias}]";
        }

        var summaryCalculatedColumns = ResolveCalculatedColumns(request.CalculatedColumns, summaryBindings);
        foreach (var calculated in summaryCalculatedColumns)
        {
            var outputAlias = $"c{outputIndex++}";
            outerSelectParts.Add($"{calculated.SqlExpression} AS [{outputAlias}]");
            resultColumns.Add(new ReportResultColumnDto
            {
                Key = calculated.Alias,
                ColumnName = outputAlias,
                DisplayName = calculated.Alias,
                DataType = "number",
                SourceType = "calculated"
            });

            sortExpressions[calculated.Alias] = calculated.SqlExpression;
        }

        if (innerSelectParts.Count == 0)
        {
            throw new ValidationException("Choose at least one group column or aggregate.");
        }

        var innerSql = $@"
SELECT {string.Join(", ", innerSelectParts)}
{fromSql}
{(groupByParts.Count == 0 ? string.Empty : $"GROUP BY {string.Join(", ", groupByParts)}")}";

        var orderSql = BuildOutputOrderByClause(request.SortColumn, request.SortDescending, sortExpressions);
        if (string.IsNullOrWhiteSpace(orderSql) && groupColumns.Count > 0)
        {
            orderSql = $" ORDER BY {string.Join(", ", Enumerable.Range(0, groupColumns.Count).Select(index => $"summary.[g{index}]"))}";
        }

        var sql = $@"
SELECT TOP (@MaxRows) {string.Join(", ", outerSelectParts)}
FROM
(
{innerSql}
) summary
{orderSql};";

        var rows = connection.Query(sql, parameters)
            .Select(row =>
            {
                var values = (IDictionary<string, object?>)row;
                return values.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            })
            .ToList();

        return new ReportResultDto
        {
            TableName = $"{rootTable.Key} Summary",
            Columns = resultColumns,
            Rows = rows,
            RowCount = rows.Count,
            MaxRows = maxRows,
            IsGrouped = true,
            GeneratedSql = request.IncludeSqlPreview ? sql : string.Empty
        };
    }

    private IReadOnlyList<ReportTableLinkDto> ResolveRequestedLinks(IDbConnection connection, IEnumerable<ReportJoinRequest>? joinRequests)
    {
        var allLinks = GetLinks(connection)
            .ToDictionary(link => link.LinkKey, StringComparer.OrdinalIgnoreCase);
        var selectedLinks = new List<ReportTableLinkDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in (joinRequests ?? Enumerable.Empty<ReportJoinRequest>()).OrderBy(join => join.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(request.LinkKey))
            {
                continue;
            }

            if (!allLinks.TryGetValue(request.LinkKey.Trim(), out var link))
            {
                throw new ValidationException($"Join link '{request.LinkKey}' was not found.");
            }

            if (seen.Add(link.LinkKey))
            {
                selectedLinks.Add(link);
            }
        }

        return selectedLinks;
    }

    private void ApplyJoin(
        IDbConnection connection,
        ReportTableLinkDto link,
        IDictionary<string, string> aliasByTableKey,
        IDictionary<string, ReportTableDto> activeTables,
        ICollection<string> activeTableOrder,
        ICollection<string> joinSqlParts)
    {
        var sourceIsActive = aliasByTableKey.TryGetValue(link.SourceTableKey, out var sourceAlias);
        var targetIsActive = aliasByTableKey.TryGetValue(link.TargetTableKey, out var targetAlias);

        if (sourceIsActive == targetIsActive)
        {
            throw new ValidationException($"Join '{link.LinkName}' could not be applied. Add links in a connected order.");
        }

        if (sourceIsActive)
        {
            var targetTable = activeTables.TryGetValue(link.TargetTableKey, out var existingTarget)
                ? existingTarget
                : GetTable(connection, link.TargetTableKey)
                    ?? throw new NotFoundException($"Join target table '{link.TargetTableKey}' was not found.");

            var newAlias = $"t{aliasByTableKey.Count}";
            aliasByTableKey[link.TargetTableKey] = newAlias;
            activeTables[link.TargetTableKey] = targetTable;
            activeTableOrder.Add(link.TargetTableKey);
            joinSqlParts.Add(
                $"{GetJoinKeyword(link.JoinType)} {Quote(targetTable.SchemaName)}.{Quote(targetTable.TableName)} {newAlias} ON {sourceAlias}.{Quote(link.SourceColumnName)} = {newAlias}.{Quote(link.TargetColumnName)}");
            return;
        }

        var sourceTable = activeTables.TryGetValue(link.SourceTableKey, out var existingSource)
            ? existingSource
            : GetTable(connection, link.SourceTableKey)
                ?? throw new NotFoundException($"Join source table '{link.SourceTableKey}' was not found.");

        var alias = $"t{aliasByTableKey.Count}";
        aliasByTableKey[link.SourceTableKey] = alias;
        activeTables[link.SourceTableKey] = sourceTable;
        activeTableOrder.Add(link.SourceTableKey);
        joinSqlParts.Add(
            $"{GetJoinKeyword(link.JoinType)} {Quote(sourceTable.SchemaName)}.{Quote(sourceTable.TableName)} {alias} ON {alias}.{Quote(link.SourceColumnName)} = {targetAlias}.{Quote(link.TargetColumnName)}");
    }

    private static List<ReportColumnDto> ResolveSelectedColumns(
        RunReportRequest request,
        IReadOnlyList<ReportColumnDto> availableColumns,
        string rootTableKey)
    {
        var columnsByKey = availableColumns.ToDictionary(column => column.Key, StringComparer.OrdinalIgnoreCase);
        var columnsByQualifiedName = availableColumns.ToDictionary(column => column.QualifiedColumnName, StringComparer.OrdinalIgnoreCase);
        var uniqueColumnsByName = availableColumns
            .GroupBy(column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var requestedIdentifiers = (request.Columns ?? new List<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedIdentifiers.Count == 0)
        {
            return availableColumns
                .Where(column => string.Equals(column.TableKey, rootTableKey, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        var selectedColumns = new List<ReportColumnDto>();
        foreach (var identifier in requestedIdentifiers)
        {
            selectedColumns.Add(ResolveColumnReference(
                identifier,
                null,
                columnsByKey,
                columnsByQualifiedName,
                uniqueColumnsByName,
                "selected"));
        }

        return selectedColumns;
    }

    private static Dictionary<string, (string SqlExpression, bool SupportsArithmetic)> BuildFormulaBindings(
        IReadOnlyList<ReportColumnDto> availableColumns,
        IReadOnlyDictionary<string, string> aliasByTableKey,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByDisplayName)
    {
        var bindings = new Dictionary<string, (string SqlExpression, bool SupportsArithmetic)>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in availableColumns)
        {
            AddFormulaBindings(
                bindings,
                column,
                $"{aliasByTableKey[column.TableKey]}.{Quote(column.ColumnName)}",
                uniqueColumnsByDisplayName,
                uniqueColumnsByName);
        }

        return bindings;
    }

    private static IReadOnlyList<(string Alias, string SqlExpression)> ResolveCalculatedColumns(
        IEnumerable<ReportCalculatedColumnRequest>? requests,
        IReadOnlyDictionary<string, (string SqlExpression, bool SupportsArithmetic)> bindings)
    {
        var calculatedColumns = new List<(string Alias, string SqlExpression)>();
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests ?? Enumerable.Empty<ReportCalculatedColumnRequest>())
        {
            if (string.IsNullOrWhiteSpace(request.Expression))
            {
                continue;
            }

            var alias = string.IsNullOrWhiteSpace(request.Alias)
                ? $"Calculated {calculatedColumns.Count + 1}"
                : request.Alias.Trim();

            if (!aliases.Add(alias))
            {
                throw new ValidationException($"Calculated field '{alias}' is duplicated.");
            }

            calculatedColumns.Add((
                alias,
                ReportBuilderFormulaSqlCompiler.CompileArithmeticExpression(request.Expression, bindings, $"Calculated field '{alias}'")));
        }

        return calculatedColumns;
    }

    private static IReadOnlyList<(ReportColumnDto Column, string SqlExpression)> ResolveGroupColumns(
        IEnumerable<string>? identifiers,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByDisplayName,
        IReadOnlyDictionary<string, string> aliasByTableKey)
    {
        var result = new List<(ReportColumnDto Column, string SqlExpression)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var identifier in identifiers ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                continue;
            }

            var column = ResolveAdvancedColumnReference(
                identifier,
                identifier,
                columnsByKey,
                columnsByQualifiedName,
                uniqueColumnsByName,
                uniqueColumnsByDisplayName,
                "group");

            if (!seen.Add(column.Key))
            {
                continue;
            }

            result.Add((column, $"{aliasByTableKey[column.TableKey]}.{Quote(column.ColumnName)}"));
        }

        return result;
    }

    private static IReadOnlyList<(string Alias, string AggregateType, string SqlExpression, string DataType)> ResolveAggregateColumns(
        IEnumerable<ReportAggregateRequest>? requests,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByDisplayName,
        IReadOnlyDictionary<string, string> aliasByTableKey)
    {
        var result = new List<(string Alias, string AggregateType, string SqlExpression, string DataType)>();
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests ?? Enumerable.Empty<ReportAggregateRequest>())
        {
            if (string.IsNullOrWhiteSpace(request.AggregateType))
            {
                continue;
            }

            var aggregateType = NormalizeAggregateType(request.AggregateType);
            ReportColumnDto? sourceColumn = null;
            string sourceExpression;

            if (aggregateType == "COUNT" && string.IsNullOrWhiteSpace(request.SourceIdentifier))
            {
                sourceExpression = "1";
            }
            else
            {
                sourceColumn = ResolveAdvancedColumnReference(
                    request.SourceIdentifier,
                    request.SourceIdentifier,
                    columnsByKey,
                    columnsByQualifiedName,
                    uniqueColumnsByName,
                    uniqueColumnsByDisplayName,
                    "aggregate");

                if ((aggregateType == "SUM" || aggregateType == "AVG")
                    && !string.Equals(sourceColumn.DataType, "number", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ValidationException($"{aggregateType} needs a numeric field. '{sourceColumn.QualifiedDisplayName}' is {sourceColumn.DataType}.");
                }

                sourceExpression = $"{aliasByTableKey[sourceColumn.TableKey]}.{Quote(sourceColumn.ColumnName)}";
            }

            var alias = string.IsNullOrWhiteSpace(request.Alias)
                ? BuildDefaultAggregateAlias(aggregateType, sourceColumn)
                : request.Alias.Trim();

            if (!aliases.Add(alias))
            {
                throw new ValidationException($"Aggregate '{alias}' is duplicated.");
            }

            result.Add((
                alias,
                aggregateType,
                BuildAggregateSqlExpression(aggregateType, sourceExpression),
                MapAggregateDataType(aggregateType, sourceColumn)));
        }

        return result;
    }

    private static ReportColumnDto ResolveAdvancedColumnReference(
        string? primaryIdentifier,
        string? fallbackIdentifier,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByDisplayName,
        string context)
    {
        var identifiers = new[] { primaryIdentifier, fallbackIdentifier }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var identifier in identifiers)
        {
            if (columnsByKey.TryGetValue(identifier, out var column))
            {
                return column;
            }

            if (columnsByQualifiedName.TryGetValue(identifier, out column))
            {
                return column;
            }

            if (uniqueColumnsByName.TryGetValue(identifier, out column))
            {
                return column;
            }

            if (uniqueColumnsByDisplayName.TryGetValue(identifier, out column))
            {
                return column;
            }
        }

        var attempted = string.Join(" / ", identifiers);
        throw new ValidationException($"The {context} column '{attempted}' is not available in the current report design.");
    }

    private static void AddFormulaBindings(
        IDictionary<string, (string SqlExpression, bool SupportsArithmetic)> bindings,
        ReportColumnDto column,
        string sqlExpression,
        IReadOnlyDictionary<string, ReportColumnDto>? uniqueColumnsByDisplayName = null,
        IReadOnlyDictionary<string, ReportColumnDto>? uniqueColumnsByName = null)
    {
        var supportsArithmetic = string.Equals(column.DataType, "number", StringComparison.OrdinalIgnoreCase);

        bindings[ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(column.Key)] = (sqlExpression, supportsArithmetic);
        bindings[ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(column.QualifiedColumnName)] = (sqlExpression, supportsArithmetic);
        bindings[ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(column.QualifiedDisplayName)] = (sqlExpression, supportsArithmetic);

        if (uniqueColumnsByName != null
            && uniqueColumnsByName.TryGetValue(column.ColumnName, out var uniqueNameColumn)
            && string.Equals(uniqueNameColumn.Key, column.Key, StringComparison.OrdinalIgnoreCase))
        {
            bindings[ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(column.ColumnName)] = (sqlExpression, supportsArithmetic);
        }

        if (uniqueColumnsByDisplayName != null
            && uniqueColumnsByDisplayName.TryGetValue(column.DisplayName, out var uniqueDisplayColumn)
            && string.Equals(uniqueDisplayColumn.Key, column.Key, StringComparison.OrdinalIgnoreCase))
        {
            bindings[ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(column.DisplayName)] = (sqlExpression, supportsArithmetic);
        }
    }

    private static void AddSortIdentifiers(
        IDictionary<string, string> sortExpressions,
        ReportColumnDto column,
        string sqlExpression)
    {
        sortExpressions[column.Key] = sqlExpression;
        sortExpressions[column.QualifiedColumnName] = sqlExpression;
        sortExpressions[column.QualifiedDisplayName] = sqlExpression;
        sortExpressions[column.DisplayName] = sqlExpression;
        sortExpressions[column.ColumnName] = sqlExpression;
    }

    private static string BuildOutputOrderByClause(
        string? sortIdentifier,
        bool isDescending,
        IReadOnlyDictionary<string, string> sortExpressions)
    {
        if (string.IsNullOrWhiteSpace(sortIdentifier))
        {
            return string.Empty;
        }

        var normalizedIdentifier = ReportBuilderFormulaSqlCompiler.NormalizeIdentifier(sortIdentifier);
        if (!sortExpressions.TryGetValue(normalizedIdentifier, out var sqlExpression))
        {
            throw new ValidationException($"Sort field '{sortIdentifier}' is not available in the current report output.");
        }

        return $" ORDER BY {sqlExpression} {(isDescending ? "DESC" : "ASC")}";
    }

    private static string NormalizeAggregateType(string aggregateType)
    {
        return (aggregateType ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "COUNT" or "COUNT_ROWS" => "COUNT",
            "SUM" => "SUM",
            "AVG" or "AVERAGE" => "AVG",
            "MIN" => "MIN",
            "MAX" => "MAX",
            _ => throw new ValidationException($"Aggregate '{aggregateType}' is not supported. Use Count, Sum, Avg, Min, or Max.")
        };
    }

    private static string BuildAggregateSqlExpression(string aggregateType, string sourceExpression)
    {
        return aggregateType switch
        {
            "COUNT" when sourceExpression == "1" => "COUNT(1)",
            "COUNT" => $"COUNT({sourceExpression})",
            "SUM" => $"SUM(TRY_CONVERT(DECIMAL(38, 10), {sourceExpression}))",
            "AVG" => $"AVG(TRY_CONVERT(DECIMAL(38, 10), {sourceExpression}))",
            "MIN" => $"MIN({sourceExpression})",
            "MAX" => $"MAX({sourceExpression})",
            _ => throw new ValidationException($"Aggregate '{aggregateType}' is not supported.")
        };
    }

    private static string BuildDefaultAggregateAlias(string aggregateType, ReportColumnDto? sourceColumn)
    {
        return sourceColumn == null
            ? "Count of Rows"
            : aggregateType switch
            {
                "COUNT" => $"Count of {sourceColumn.DisplayName}",
                "SUM" => $"Sum of {sourceColumn.DisplayName}",
                "AVG" => $"Average of {sourceColumn.DisplayName}",
                "MIN" => $"Minimum of {sourceColumn.DisplayName}",
                "MAX" => $"Maximum of {sourceColumn.DisplayName}",
                _ => $"{aggregateType} {sourceColumn.DisplayName}"
            };
    }

    private static string MapAggregateDataType(string aggregateType, ReportColumnDto? sourceColumn)
    {
        return aggregateType switch
        {
            "COUNT" or "SUM" or "AVG" => "number",
            "MIN" or "MAX" when sourceColumn != null => sourceColumn.DataType,
            _ => "number"
        };
    }

    private static string BuildFilterClause(
        ReportFilterRequest filter,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, string> aliasByTableKey,
        DynamicParameters parameters,
        ref int parameterIndex)
    {
        if ((string.IsNullOrWhiteSpace(filter.ColumnKey) && string.IsNullOrWhiteSpace(filter.ColumnName))
            || string.IsNullOrWhiteSpace(filter.Operator))
        {
            return string.Empty;
        }

        var column = ResolveColumnReference(
            filter.ColumnKey,
            filter.ColumnName,
            columnsByKey,
            columnsByQualifiedName,
            uniqueColumnsByName,
            "filter");

        var quotedColumn = $"{aliasByTableKey[column.TableKey]}.{Quote(column.ColumnName)}";
        var op = NormalizeOperator(filter.Operator);
        if (op == "is_null")
        {
            return $"{quotedColumn} IS NULL";
        }

        if (op == "is_not_null")
        {
            return $"{quotedColumn} IS NOT NULL";
        }

        if (op == "between")
        {
            if (string.IsNullOrWhiteSpace(filter.Value) || string.IsNullOrWhiteSpace(filter.ValueTo))
            {
                throw new ValidationException($"{column.QualifiedDisplayName} needs both From and To values for Between.");
            }

            var fromParameter = $"p{parameterIndex++}";
            var toParameter = $"p{parameterIndex++}";
            parameters.Add(fromParameter, ConvertFilterValue(column, filter.Value.Trim()));
            parameters.Add(toParameter, ConvertFilterValue(column, filter.ValueTo.Trim()));
            return $"{quotedColumn} BETWEEN @{fromParameter} AND @{toParameter}";
        }

        if (string.IsNullOrWhiteSpace(filter.Value))
        {
            throw new ValidationException($"{column.QualifiedDisplayName} needs a filter value.");
        }

        var parameterName = $"p{parameterIndex++}";
        switch (op)
        {
            case "equals":
                parameters.Add(parameterName, ConvertFilterValue(column, filter.Value.Trim()));
                return $"{quotedColumn} = @{parameterName}";
            case "not_equals":
                parameters.Add(parameterName, ConvertFilterValue(column, filter.Value.Trim()));
                return $"{quotedColumn} <> @{parameterName}";
            case "greater_than":
                parameters.Add(parameterName, ConvertFilterValue(column, filter.Value.Trim()));
                return $"{quotedColumn} > @{parameterName}";
            case "greater_or_equal":
                parameters.Add(parameterName, ConvertFilterValue(column, filter.Value.Trim()));
                return $"{quotedColumn} >= @{parameterName}";
            case "less_than":
                parameters.Add(parameterName, ConvertFilterValue(column, filter.Value.Trim()));
                return $"{quotedColumn} < @{parameterName}";
            case "less_or_equal":
                parameters.Add(parameterName, ConvertFilterValue(column, filter.Value.Trim()));
                return $"{quotedColumn} <= @{parameterName}";
            case "contains":
                parameters.Add(parameterName, $"%{EscapeLikeValue(filter.Value.Trim())}%");
                return $"CONVERT(NVARCHAR(MAX), {quotedColumn}) LIKE @{parameterName} ESCAPE '\\'";
            case "starts_with":
                parameters.Add(parameterName, $"{EscapeLikeValue(filter.Value.Trim())}%");
                return $"CONVERT(NVARCHAR(MAX), {quotedColumn}) LIKE @{parameterName} ESCAPE '\\'";
            case "ends_with":
                parameters.Add(parameterName, $"%{EscapeLikeValue(filter.Value.Trim())}");
                return $"CONVERT(NVARCHAR(MAX), {quotedColumn}) LIKE @{parameterName} ESCAPE '\\'";
            default:
                throw new ValidationException($"Filter operator '{filter.Operator}' is not supported.");
        }
    }

    private static string BuildOrderByClause(
        RunReportRequest request,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, string> aliasByTableKey)
    {
        if (string.IsNullOrWhiteSpace(request.SortColumn))
        {
            return string.Empty;
        }

        var column = ResolveColumnReference(
            request.SortColumn,
            request.SortColumn,
            columnsByKey,
            columnsByQualifiedName,
            uniqueColumnsByName,
            "sort");

        return $" ORDER BY {aliasByTableKey[column.TableKey]}.{Quote(column.ColumnName)} {(request.SortDescending ? "DESC" : "ASC")}";
    }

    private static ReportColumnDto ResolveColumnReference(
        string? primaryIdentifier,
        string? fallbackIdentifier,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        string context)
    {
        var identifiers = new[] { primaryIdentifier, fallbackIdentifier }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var identifier in identifiers)
        {
            if (columnsByKey.TryGetValue(identifier, out var column))
            {
                return column;
            }

            if (columnsByQualifiedName.TryGetValue(identifier, out column))
            {
                return column;
            }

            if (uniqueColumnsByName.TryGetValue(identifier, out column))
            {
                return column;
            }
        }

        var attempted = string.Join(" / ", identifiers);
        throw new ValidationException($"The {context} column '{attempted}' is not available in the current report design.");
    }

    private static object ConvertFilterValue(ReportColumnDto column, string rawValue)
    {
        var sqlType = column.SqlDataType.Trim().ToLowerInvariant();
        return sqlType switch
        {
            "tinyint" => ParseByte(rawValue, column.QualifiedDisplayName),
            "smallint" => ParseShort(rawValue, column.QualifiedDisplayName),
            "int" => ParseInt(rawValue, column.QualifiedDisplayName),
            "bigint" => ParseLong(rawValue, column.QualifiedDisplayName),
            "bit" => ParseBoolean(rawValue),
            "decimal" or "numeric" or "money" or "smallmoney" => ParseDecimal(rawValue, column.QualifiedDisplayName),
            "float" or "real" => ParseDouble(rawValue, column.QualifiedDisplayName),
            "date" or "datetime" or "datetime2" or "smalldatetime" => ParseDateTime(rawValue, column.QualifiedDisplayName),
            "datetimeoffset" => ParseDateTimeOffset(rawValue, column.QualifiedDisplayName),
            "time" => ParseTime(rawValue, column.QualifiedDisplayName),
            "uniqueidentifier" => ParseGuid(rawValue, column.QualifiedDisplayName),
            _ => rawValue
        };
    }

    private IReadOnlyList<ReportTableLinkDto> GetLinks(IDbConnection connection)
    {
        var uniqueColumns = GetUniqueColumns(connection);
        var customLinks = connection.Query<ReportTableLinkDto>(
                @"
SELECT Id,
       CONCAT('CUSTOM:', CAST(Id AS NVARCHAR(20))) AS LinkKey,
       LinkName,
       SourceTableKey,
       SourceColumnName,
       TargetTableKey,
       TargetColumnName,
       JoinType,
       CAST(0 AS bit) AS IsSystemDefined
FROM dbo.ReportBuilderTableLinks
WHERE IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY LinkName, SourceTableKey, TargetTableKey, SourceColumnName, TargetColumnName;",
                new { _tenantContext.TenantId })
            .ToList();

        var systemLinks = connection.Query<ReportTableLinkDto>(
                @"
SELECT CAST(NULL AS INT) AS Id,
       CONCAT('FK:', fk.name, ':', ps.name, '.', pt.name, '.', pc.name, '=>', rs.name, '.', rt.name, '.', rc.name) AS LinkKey,
       fk.name AS LinkName,
       CONCAT(ps.name, '.', pt.name) AS SourceTableKey,
       pc.name AS SourceColumnName,
       CONCAT(rs.name, '.', rt.name) AS TargetTableKey,
       rc.name AS TargetColumnName,
       CAST('LEFT' AS NVARCHAR(20)) AS JoinType,
       CAST(1 AS bit) AS IsSystemDefined
FROM sys.foreign_key_columns fkc
INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables pt ON pt.object_id = fkc.parent_object_id
INNER JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id
                              AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.tables rt ON rt.object_id = fkc.referenced_object_id
INNER JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id
                              AND rc.column_id = fkc.referenced_column_id
WHERE pt.is_ms_shipped = 0
  AND rt.is_ms_shipped = 0
  AND ps.name NOT IN ('sys', 'INFORMATION_SCHEMA')
  AND rs.name NOT IN ('sys', 'INFORMATION_SCHEMA')
ORDER BY fk.name, ps.name, pt.name, pc.name;")
            .ToList();

        var links = customLinks
            .Concat(systemLinks)
            .Where(link => !IsRestrictedTableKey(link.SourceTableKey)
                && !IsRestrictedTableKey(link.TargetTableKey))
            .ToList();

        foreach (var link in links)
        {
            PrepareLink(link);
            PopulateLinkCardinality(link, uniqueColumns);
        }

        return links
            .OrderBy(link => link.SourceTableDisplayName)
            .ThenBy(link => link.TargetTableDisplayName)
            .ThenBy(link => link.LinkName)
            .ToList();
    }

    private static ReportTableDto? GetTable(IDbConnection connection, string tableKey)
    {
        var (schemaName, tableName) = ParseTableKey(tableKey);
        if (string.IsNullOrWhiteSpace(schemaName)
            || string.IsNullOrWhiteSpace(tableName)
            || IsRestrictedTable(schemaName, tableName))
        {
            return null;
        }

        var table = connection.QueryFirstOrDefault<ReportTableDto>(
            @"
SELECT CONCAT(s.name, '.', t.name) AS [Key],
       s.name AS SchemaName,
       t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND s.name = @SchemaName
  AND t.name = @TableName;",
            new { SchemaName = schemaName, TableName = tableName });

        if (table != null)
        {
            PrepareTable(table);
        }

        return table;
    }

    public static bool IsTableAllowedForReporting(string tableKey)
    {
        var (schemaName, tableName) = ParseTableKey(tableKey);
        return !string.IsNullOrWhiteSpace(schemaName)
            && !string.IsNullOrWhiteSpace(tableName)
            && !IsRestrictedTable(schemaName, tableName);
    }

    private static bool IsRestrictedTableKey(string tableKey)
    {
        var (schemaName, tableName) = ParseTableKey(tableKey);
        return IsRestrictedTable(schemaName, tableName);
    }

    private static bool IsRestrictedTable(string schemaName, string tableName)
        => RestrictedTableNames.Contains(tableName)
            || tableName.StartsWith("ReportBuilder", StringComparison.OrdinalIgnoreCase);

    private static void EnsureAdmin(int currentRoleId)
    {
        if (currentRoleId != (int)UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only administrators can modify report table links.");
        }
    }

    private static IReadOnlyList<ReportColumnDto> GetColumns(IDbConnection connection, ReportTableDto table)
    {
        var columns = connection.Query<ReportColumnDto>(
                @"
SELECT CONCAT(s.name, '.', t.name, '.', c.name) AS [Key],
       CONCAT(s.name, '.', t.name) AS TableKey,
       c.name AS ColumnName,
       ty.name AS SqlDataType,
       CAST(c.is_nullable AS bit) AS IsNullable,
       CASE
           WHEN ty.name IN ('nvarchar', 'nchar') AND c.max_length > 0 THEN c.max_length / 2
           WHEN c.max_length = -1 THEN -1
           ELSE c.max_length
       END AS MaxLength,
       c.column_id AS OrdinalPosition
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0
  AND s.name = @SchemaName
  AND t.name = @TableName
  AND ty.name NOT IN @UnsupportedSqlTypes
  AND LOWER(c.name) NOT LIKE '%password%'
  AND LOWER(c.name) NOT LIKE '%token%'
  AND LOWER(c.name) NOT LIKE '%secret%'
  AND LOWER(c.name) NOT LIKE '%apikey%'
  AND LOWER(c.name) NOT LIKE '%api_key%'
ORDER BY c.column_id;",
                new { table.SchemaName, table.TableName, UnsupportedSqlTypes })
            .ToList();

        foreach (var column in columns)
        {
            PrepareColumn(column, table);
        }

        return columns;
    }

    private static void PrepareTable(ReportTableDto table)
    {
        table.DisplayName = ToDisplayName(table.TableName);
    }

    private static void PrepareColumn(ReportColumnDto column, ReportTableDto table)
    {
        column.TableKey = table.Key;
        column.TableDisplayName = table.DisplayName;
        column.DisplayName = ToDisplayName(column.ColumnName);
        column.QualifiedDisplayName = $"{table.DisplayName} / {column.DisplayName}";
        column.QualifiedColumnName = $"{table.Key}.{column.ColumnName}";
        column.DataType = MapSqlType(column.SqlDataType);
    }

    private static void PrepareLink(ReportTableLinkDto link)
    {
        var (sourceSchemaName, sourceTableName) = ParseTableKey(link.SourceTableKey);
        var (targetSchemaName, targetTableName) = ParseTableKey(link.TargetTableKey);

        link.SourceSchemaName = sourceSchemaName;
        link.SourceTableName = sourceTableName;
        link.SourceTableDisplayName = ToDisplayName(sourceTableName);
        link.TargetSchemaName = targetSchemaName;
        link.TargetTableName = targetTableName;
        link.TargetTableDisplayName = ToDisplayName(targetTableName);
        link.JoinType = NormalizeJoinType(link.JoinType);
        link.Description =
            $"{link.SourceTableDisplayName}.{link.SourceColumnName} {GetJoinLabel(link.JoinType)} {link.TargetTableDisplayName}.{link.TargetColumnName}";

        if (string.IsNullOrWhiteSpace(link.LinkName))
        {
            link.LinkName = link.Description;
        }
    }

    private static (string SchemaName, string TableName) ParseTableKey(string tableKey)
    {
        var parts = (tableKey ?? string.Empty).Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => ("dbo", parts[0]),
            2 => (parts[0], parts[1]),
            _ => (string.Empty, string.Empty)
        };
    }

    private static string Quote(string identifier)
        => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string NormalizeOperator(string value)
        => new(value.Trim().Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_').ToArray());

    private static string NormalizeJoinType(string? joinType)
    {
        return (joinType ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "" or "LEFT JOIN" or "LEFT" => "LEFT",
            "INNER JOIN" or "INNER" => "INNER",
            _ => throw new ValidationException($"Join type '{joinType}' is not supported. Use LEFT or INNER.")
        };
    }

    private static string GetJoinKeyword(string joinType)
        => NormalizeJoinType(joinType) == "INNER" ? "INNER JOIN" : "LEFT JOIN";

    private static string GetJoinLabel(string joinType)
        => NormalizeJoinType(joinType) == "INNER" ? "matches" : "optionally matches";

    private static string EscapeLikeValue(string value)
        => value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal);

    private static byte ParseByte(string rawValue, string columnName)
    {
        if (byte.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid whole number.");
    }

    private static short ParseShort(string rawValue, string columnName)
    {
        if (short.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid whole number.");
    }

    private static int ParseInt(string rawValue, string columnName)
    {
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid whole number.");
    }

    private static long ParseLong(string rawValue, string columnName)
    {
        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid whole number.");
    }

    private static bool ParseBoolean(string rawValue)
    {
        return NormalizeLookupKey(rawValue) switch
        {
            "1" or "true" or "yes" or "y" or "on" => true,
            "0" or "false" or "no" or "n" or "off" => false,
            _ => throw new ValidationException($"Boolean value '{rawValue}' is invalid. Use True/False, Yes/No, or 1/0.")
        };
    }

    private static decimal ParseDecimal(string rawValue, string columnName)
    {
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid number.");
    }

    private static double ParseDouble(string rawValue, string columnName)
    {
        if (double.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || double.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid number.");
    }

    private static DateTime ParseDateTime(string rawValue, string columnName)
    {
        if (DateTime.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            || DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid date.");
    }

    private static DateTimeOffset ParseDateTimeOffset(string rawValue, string columnName)
    {
        if (DateTimeOffset.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            || DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid date/time.");
    }

    private static TimeSpan ParseTime(string rawValue, string columnName)
    {
        if (TimeSpan.TryParse(rawValue, CultureInfo.CurrentCulture, out var parsed)
            || TimeSpan.TryParse(rawValue, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid time.");
    }

    private static Guid ParseGuid(string rawValue, string columnName)
    {
        if (Guid.TryParse(rawValue, out var parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid identifier.");
    }

    private static string MapSqlType(string sqlType)
    {
        return sqlType.Trim().ToLowerInvariant() switch
        {
            "tinyint" or "smallint" or "int" or "bigint" => "Integer",
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "Decimal",
            "bit" => "Boolean",
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "Date",
            "time" => "Time",
            "uniqueidentifier" => "Identifier",
            _ => "Text"
        };
    }

    private static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new List<char>();
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0
                && char.IsUpper(current)
                && (char.IsLower(value[index - 1]) || (index + 1 < value.Length && char.IsLower(value[index + 1]))))
            {
                chars.Add(' ');
            }

            chars.Add(current == '_' ? ' ' : current);
        }

        return new string(chars.ToArray()).Trim();
    }

    private static string NormalizeLookupKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}
