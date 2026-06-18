using Dapper;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Reports;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace SpareParts.Infrastructure.Services;

public sealed partial class ReportBuilderService
{
    private static readonly JsonSerializerOptions ReportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ReportJoinGraphDto GetJoinGraph()
    {
        using var connection = _factory.CreateConnection();
        var tables = GetTables()
            .Select(table => new ReportJoinGraphNodeDto
            {
                TableKey = table.Key,
                SchemaName = table.SchemaName,
                TableName = table.TableName,
                DisplayName = table.DisplayName,
                ColumnCount = table.ColumnCount
            })
            .ToList();

        var edges = GetLinks(connection)
            .Select(link => new ReportJoinGraphEdgeDto
            {
                LinkKey = link.LinkKey,
                LinkName = link.LinkName,
                SourceTableKey = link.SourceTableKey,
                TargetTableKey = link.TargetTableKey,
                JoinType = link.JoinType,
                CardinalityLabel = link.CardinalityLabel,
                MayDuplicateRows = link.MayDuplicateRows,
                DuplicationWarning = link.DuplicationWarning,
                IsSystemDefined = link.IsSystemDefined
            })
            .ToList();

        return new ReportJoinGraphDto
        {
            Nodes = tables,
            Edges = edges
        };
    }

    public ReportSchemaInspectorDto GetSchemaInspector(string tableKey)
    {
        using var connection = _factory.CreateConnection();
        var table = GetTable(connection, tableKey)
            ?? throw new NotFoundException($"System table '{tableKey}' was not found.");

        var columns = GetColumns(connection, table)
            .ToDictionary(column => column.ColumnName, StringComparer.OrdinalIgnoreCase);

        var metadata = connection.Query(
                @"
SELECT c.name AS ColumnName,
       CAST(CASE WHEN pk.ColumnId IS NULL THEN 0 ELSE 1 END AS bit) AS IsPrimaryKey,
       CAST(CASE WHEN fk.ColumnId IS NULL THEN 0 ELSE 1 END AS bit) AS IsForeignKey
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
LEFT JOIN
(
    SELECT ic.object_id, ic.column_id AS ColumnId
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
) pk ON pk.object_id = c.object_id AND pk.ColumnId = c.column_id
LEFT JOIN
(
    SELECT parent_object_id AS object_id, parent_column_id AS ColumnId
    FROM sys.foreign_key_columns
    UNION
    SELECT referenced_object_id AS object_id, referenced_column_id AS ColumnId
    FROM sys.foreign_key_columns
) fk ON fk.object_id = c.object_id AND fk.ColumnId = c.column_id
WHERE s.name = @SchemaName
  AND t.name = @TableName;",
                new { table.SchemaName, table.TableName })
            .Select(row => (IDictionary<string, object?>)row)
            .ToDictionary(
                row => Convert.ToString(row["ColumnName"], CultureInfo.InvariantCulture) ?? string.Empty,
                row => new
                {
                    IsPrimaryKey = ToBoolean(row["IsPrimaryKey"]),
                    IsForeignKey = ToBoolean(row["IsForeignKey"])
                },
                StringComparer.OrdinalIgnoreCase);

        var tableDescription = connection.ExecuteScalar<string?>(
            @"
SELECT CAST(ep.value AS NVARCHAR(4000))
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
LEFT JOIN sys.extended_properties ep
    ON ep.major_id = t.object_id
   AND ep.minor_id = 0
   AND ep.name = 'MS_Description'
WHERE s.name = @SchemaName
  AND t.name = @TableName;",
            new { table.SchemaName, table.TableName }) ?? string.Empty;

        var estimatedRowCount = connection.ExecuteScalar<long>(
            @"
SELECT ISNULL(SUM(ps.row_count), 0)
FROM sys.dm_db_partition_stats ps
INNER JOIN sys.tables t ON t.object_id = ps.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @SchemaName
  AND t.name = @TableName
  AND ps.index_id IN (0, 1);",
            new { table.SchemaName, table.TableName });

        var relations = GetLinks(connection)
            .Where(link =>
                string.Equals(link.SourceTableKey, table.Key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(link.TargetTableKey, table.Key, StringComparison.OrdinalIgnoreCase))
            .Select(link =>
            {
                var isOutgoing = string.Equals(link.SourceTableKey, table.Key, StringComparison.OrdinalIgnoreCase);
                return new ReportSchemaForeignKeyDto
                {
                    LinkName = link.LinkName,
                    ColumnName = isOutgoing ? link.SourceColumnName : link.TargetColumnName,
                    RelatedTableKey = isOutgoing ? link.TargetTableKey : link.SourceTableKey,
                    RelatedTableDisplayName = isOutgoing ? link.TargetTableDisplayName : link.SourceTableDisplayName,
                    RelatedColumnName = isOutgoing ? link.TargetColumnName : link.SourceColumnName,
                    Direction = isOutgoing ? "Outgoing" : "Incoming",
                    CardinalityLabel = isOutgoing
                        ? link.CardinalityLabel
                        : $"{link.TargetCardinality} -> {link.SourceCardinality}"
                };
            })
            .OrderBy(item => item.Direction)
            .ThenBy(item => item.RelatedTableDisplayName)
            .ToList();

        return new ReportSchemaInspectorDto
        {
            TableKey = table.Key,
            DisplayName = table.DisplayName,
            SchemaName = table.SchemaName,
            TableName = table.TableName,
            Description = tableDescription,
            EstimatedRowCount = estimatedRowCount,
            Columns = columns.Values
                .OrderBy(column => column.OrdinalPosition)
                .Select(column =>
                {
                    metadata.TryGetValue(column.ColumnName, out var info);
                    return new ReportSchemaColumnDto
                    {
                        ColumnName = column.ColumnName,
                        DisplayName = column.DisplayName,
                        SqlDataType = column.SqlDataType,
                        DataType = column.DataType,
                        IsNullable = column.IsNullable,
                        IsPrimaryKey = info?.IsPrimaryKey ?? false,
                        IsForeignKey = info?.IsForeignKey ?? false,
                        MaxLength = column.MaxLength,
                        SampleValuesText = GetSampleValuesText(connection, table, column.ColumnName)
                    };
                })
                .ToList(),
            Relations = relations
        };
    }

    public ReportSecurityOptionsDto GetSecurityOptions(int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        var (baseCurrencyCode, counterCurrencyCode) = GetConfiguredCurrencyContext(connection, _tenantContext.TenantId);
        var roles = connection.Query<RoleSecurityOptionDto>(
            @"SELECT Id AS RoleId, Name AS RoleName FROM dbo.Roles WHERE IsActive = 1 ORDER BY Name;")
            .ToList();

        return new ReportSecurityOptionsDto
        {
            CurrentRoleId = currentRoleId,
            DefaultBaseCurrencyCode = baseCurrencyCode,
            DefaultCounterCurrencyCode = counterCurrencyCode,
            Roles = roles,
            CurrencyCodes = GetAvailableCurrencyCodes(connection, baseCurrencyCode, counterCurrencyCode, _tenantContext.TenantId)
        };
    }

    public IReadOnlyList<ReportSavedReportSummaryDto> GetSavedReports(int currentUserId, int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        var summaries = connection.Query<ReportSavedReportSummaryDto>(
                @"
SELECT Id,
       [Name],
       ISNULL([Description], N'') AS [Description],
       TableKey,
       DefaultExportFormat,
       PreferredChartType,
       IsSensitive,
       CreatedAt,
       ModifiedAt,
       CreatedByUserId
FROM dbo.ReportBuilderSavedReports
WHERE IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY [Name], Id;",
                new { _tenantContext.TenantId })
            .ToList();

        var reportIds = summaries.Select(summary => summary.Id).ToArray();
        var favorites = GetFavoriteReportIds(connection, currentUserId);
        var accessLookup = GetSavedReportRoleAccessLookup(connection, reportIds);

        foreach (var summary in summaries)
        {
            summary.TableDisplayName = ToDisplayName(ParseTableKey(summary.TableKey).TableName);
            ApplySavedReportPermissions(summary, accessLookup.TryGetValue(summary.Id, out var accessRules) ? accessRules : [], favorites, currentUserId, currentRoleId);
        }

        return summaries
            .Where(summary => summary.CanView)
            .OrderByDescending(summary => summary.IsFavorite)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ReportSavedReportDetailDto GetSavedReport(int id, int currentUserId, int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        return GetSavedReportDetail(connection, id, currentUserId, currentRoleId, requireEdit: false, _tenantContext.TenantId);
    }

    public ReportSavedReportDetailDto SaveReport(SaveReportDefinitionRequest request, int currentUserId, int currentRoleId)
    {
        if (request == null)
        {
            throw new ValidationException("Saved report payload is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Report name is required.");
        }

        var layout = NormalizeLayout(request.Layout, request.PreferredChartType);
        if (string.IsNullOrWhiteSpace(layout.TableKey))
        {
            throw new ValidationException("Choose a base table before saving the report.");
        }

        using var connection = _factory.CreateConnection();
        var existing = request.Id is > 0
            ? GetSavedReportDetail(connection, request.Id.Value, currentUserId, currentRoleId, requireEdit: true, _tenantContext.TenantId)
            : null;

        var validRoleIds = connection.Query<int>("SELECT Id FROM dbo.Roles WHERE IsActive = 1;").ToHashSet();
        var accessRules = NormalizeAccessRules(request.AccessRules, validRoleIds);
        var definitionJson = JsonSerializer.Serialize(layout, ReportJsonOptions);
        var normalizedExportFormat = NormalizeExportFormat(request.DefaultExportFormat);
        var normalizedChartType = NormalizeChartType(request.PreferredChartType);

        int reportId;
        if (existing != null)
        {
            connection.Execute(
                @"
UPDATE dbo.ReportBuilderSavedReports
SET [Name] = @Name,
    [Description] = @Description,
    TableKey = @TableKey,
    DefinitionJson = @DefinitionJson,
    DefaultExportFormat = @DefaultExportFormat,
    PreferredChartType = @PreferredChartType,
    IsSensitive = @IsSensitive,
    ModifiedByUserId = @ModifiedByUserId,
    ModifiedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND (@TenantId = 0 OR TenantId = @TenantId);",
                new
                {
                    Id = existing.Id,
                    Name = request.Name.Trim(),
                    Description = NormalizeOptionalText(request.Description),
                    TableKey = layout.TableKey!.Trim(),
                    DefinitionJson = definitionJson,
                    DefaultExportFormat = normalizedExportFormat,
                    PreferredChartType = normalizedChartType,
                    request.IsSensitive,
                    ModifiedByUserId = currentUserId,
                    _tenantContext.TenantId
                });

            reportId = existing.Id;
        }
        else
        {
            var tenantId = _tenantContext.TenantId > 0 ? (int?)_tenantContext.TenantId : null;
            reportId = connection.ExecuteScalar<int>(
                @"
INSERT INTO dbo.ReportBuilderSavedReports
(
    [Name],
    [Description],
    TableKey,
    DefinitionJson,
    DefaultExportFormat,
    PreferredChartType,
    IsSensitive,
    IsActive,
    CreatedByUserId,
    TenantId
)
VALUES
(
    @Name,
    @Description,
    @TableKey,
    @DefinitionJson,
    @DefaultExportFormat,
    @PreferredChartType,
    @IsSensitive,
    1,
    @CreatedByUserId,
    @TenantId
);

SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    Name = request.Name.Trim(),
                    Description = NormalizeOptionalText(request.Description),
                    TableKey = layout.TableKey!.Trim(),
                    DefinitionJson = definitionJson,
                    DefaultExportFormat = normalizedExportFormat,
                    PreferredChartType = normalizedChartType,
                    request.IsSensitive,
                    CreatedByUserId = currentUserId,
                    TenantId = tenantId
                });
        }

        connection.Execute("DELETE FROM dbo.ReportBuilderSavedReportRoles WHERE ReportId = @ReportId;", new { ReportId = reportId });
        foreach (var accessRule in accessRules)
        {
            connection.Execute(
                @"
INSERT INTO dbo.ReportBuilderSavedReportRoles (ReportId, RoleId, CanView, CanEdit, CanExport)
VALUES (@ReportId, @RoleId, @CanView, @CanEdit, @CanExport);",
                new
                {
                    ReportId = reportId,
                    accessRule.RoleId,
                    accessRule.CanView,
                    accessRule.CanEdit,
                    accessRule.CanExport
                });
        }

        SetFavoriteInternal(connection, reportId, currentUserId, request.IsFavorite);
        return GetSavedReportDetail(connection, reportId, currentUserId, currentRoleId, requireEdit: false, _tenantContext.TenantId);
    }

    public void DeleteSavedReport(int id, int currentUserId, int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        _ = GetSavedReportDetail(connection, id, currentUserId, currentRoleId, requireEdit: true, _tenantContext.TenantId);

        var affected = connection.Execute(
            @"
UPDATE dbo.ReportBuilderSavedReports
SET IsActive = 0,
    ModifiedByUserId = @ModifiedByUserId,
    ModifiedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, ModifiedByUserId = currentUserId, _tenantContext.TenantId });

        if (affected == 0)
        {
            throw new NotFoundException($"Saved report '{id}' was not found.");
        }
    }

    public ReportSavedReportSummaryDto SetFavorite(int id, bool isFavorite, int currentUserId, int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        _ = GetSavedReportDetail(connection, id, currentUserId, currentRoleId, requireEdit: false, _tenantContext.TenantId);
        SetFavoriteInternal(connection, id, currentUserId, isFavorite);
        return GetSavedReportDetail(connection, id, currentUserId, currentRoleId, requireEdit: false, _tenantContext.TenantId);
    }

    public BackgroundReportRunSummaryDto QueueBackgroundRun(QueueBackgroundReportRunRequest request, int currentUserId, int currentRoleId)
    {
        if (request == null)
        {
            throw new ValidationException("Background report payload is required.");
        }

        var layout = NormalizeLayout(request.Layout, chartType: null);
        if (string.IsNullOrWhiteSpace(layout.TableKey))
        {
            throw new ValidationException("Choose a base table before queueing the report.");
        }

        var reportName = string.IsNullOrWhiteSpace(request.ReportName)
            ? $"Background {ToDisplayName(ParseTableKey(layout.TableKey).TableName)}"
            : request.ReportName.Trim();
        var requestJson = JsonSerializer.Serialize(layout, ReportJsonOptions);

        using var connection = _factory.CreateConnection();
        var runId = connection.ExecuteScalar<int>(
            @"
INSERT INTO dbo.ReportBuilderBackgroundRuns
(
    ReportName,
    RequestedByUserId,
    RequestedByRoleId,
    [Status],
    ProgressPercent,
    RequestJson
)
VALUES
(
    @ReportName,
    @RequestedByUserId,
    @RequestedByRoleId,
    N'Queued',
    0,
    @RequestJson
);

SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                ReportName = reportName,
                RequestedByUserId = currentUserId,
                RequestedByRoleId = currentRoleId,
                RequestJson = requestJson
            });

        return new BackgroundReportRunSummaryDto
        {
            Id = runId,
            ReportName = reportName,
            Status = "Queued",
            ProgressPercent = 0,
            SubmittedAt = DateTime.UtcNow,
            Summary = "Background report queued.",
            ResultAvailable = false,
            CanExport = false
        };
    }

    public int ProcessQueuedBackgroundRuns(int maxRuns = 3)
    {
        var runLimit = Math.Clamp(maxRuns, 1, 20);
        var processedCount = 0;

        while (processedCount < runLimit)
        {
            var runId = ClaimNextBackgroundRun();
            if (runId is not > 0)
            {
                break;
            }

            ExecuteBackgroundRun(runId.Value);
            processedCount++;
        }

        return processedCount;
    }

    public int RequeueStaleBackgroundRuns()
    {
        using var connection = _factory.CreateConnection();
        return connection.Execute(
            @"
UPDATE dbo.ReportBuilderBackgroundRuns
SET [Status] = N'Queued',
    ProgressPercent = 0,
    StartedAt = NULL,
    ErrorMessage = NULL,
    [Summary] = N'Background report queued again after an interrupted run.'
WHERE [Status] = N'Running'
  AND StartedAt < DATEADD(MINUTE, -60, SYSUTCDATETIME());");
    }

    public IReadOnlyList<BackgroundReportRunSummaryDto> GetBackgroundRuns(int currentUserId, int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        var isAdmin = currentRoleId == (int)UserRole.Admin;
        var sql = isAdmin
            ? @"
SELECT TOP (60)
       Id,
       ReportName,
       [Status] AS [Status],
       ProgressPercent,
       CreatedAt AS SubmittedAt,
       StartedAt,
       CompletedAt,
       ResultRowCount AS 'RowCount',
       ISNULL([Summary], N'') AS [Summary],
       ErrorMessage,
       CAST(CASE WHEN ResultJson IS NULL OR LTRIM(RTRIM(ResultJson)) = N'' THEN 0 ELSE 1 END AS bit) AS ResultAvailable,
       CAST(CASE WHEN ResultJson IS NULL OR LTRIM(RTRIM(ResultJson)) = N'' THEN 0 ELSE 1 END AS bit) AS CanExport
FROM dbo.ReportBuilderBackgroundRuns
ORDER BY Id DESC;"
            : @"
SELECT TOP (60)
       Id,
       ReportName,
       [Status] AS [Status],
       ProgressPercent,
       CreatedAt AS SubmittedAt,
       StartedAt,
       CompletedAt,
       ResultRowCount AS 'RowCount',
       ISNULL([Summary], N'') AS [Summary],
       ErrorMessage,
       CAST(CASE WHEN ResultJson IS NULL OR LTRIM(RTRIM(ResultJson)) = N'' THEN 0 ELSE 1 END AS bit) AS ResultAvailable,
       CAST(CASE WHEN ResultJson IS NULL OR LTRIM(RTRIM(ResultJson)) = N'' THEN 0 ELSE 1 END AS bit) AS CanExport
FROM dbo.ReportBuilderBackgroundRuns
WHERE RequestedByUserId = @UserId
ORDER BY Id DESC;";

        return connection.Query<BackgroundReportRunSummaryDto>(sql, new { UserId = currentUserId }).ToList();
    }

    public ReportResultDto GetBackgroundRunResult(int id, int currentUserId, int currentRoleId)
    {
        using var connection = _factory.CreateConnection();
        var row = connection.QuerySingleOrDefault(
            @"
SELECT RequestedByUserId,
       [Status],
       ResultJson
FROM dbo.ReportBuilderBackgroundRuns
WHERE Id = @Id;",
            new { Id = id });

        if (row == null)
        {
            throw new NotFoundException($"Background report '{id}' was not found.");
        }

        var values = (IDictionary<string, object?>)row;
        var requestedByUserId = ToInt32(values["RequestedByUserId"]);
        var status = Convert.ToString(values["Status"], CultureInfo.InvariantCulture) ?? string.Empty;
        var resultJson = Convert.ToString(values["ResultJson"], CultureInfo.InvariantCulture);
        var isAdmin = currentRoleId == (int)UserRole.Admin;

        if (!isAdmin && requestedByUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You do not have permission to open this background report.");
        }

        if (!string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(resultJson))
        {
            throw new ValidationException("This background report is not ready yet.");
        }

        return JsonSerializer.Deserialize<ReportResultDto>(resultJson, ReportJsonOptions)
            ?? throw new ValidationException("The background report result is empty.");
    }

    private int? ClaimNextBackgroundRun()
    {
        using var connection = _factory.CreateConnection();
        return connection.QueryFirstOrDefault<int?>(
            @"
;WITH NextRun AS
(
    SELECT TOP (1) *
    FROM dbo.ReportBuilderBackgroundRuns WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE [Status] = N'Queued'
    ORDER BY Id
)
UPDATE NextRun
SET [Status] = N'Running',
    ProgressPercent = 15,
    StartedAt = SYSUTCDATETIME(),
    ErrorMessage = NULL
OUTPUT INSERTED.Id;");
    }

    private void ExecuteBackgroundRun(int runId)
    {
        using var connection = _factory.CreateConnection();
        try
        {
            var row = connection.QuerySingleOrDefault(
                "SELECT RequestJson FROM dbo.ReportBuilderBackgroundRuns WHERE Id = @Id;",
                new { Id = runId });
            if (row == null)
            {
                return;
            }

            var values = (IDictionary<string, object?>)row;
            var requestJson = Convert.ToString(values["RequestJson"], CultureInfo.InvariantCulture) ?? "{}";
            var layout = JsonSerializer.Deserialize<SavedReportLayoutDto>(requestJson, ReportJsonOptions)
                ?? new SavedReportLayoutDto();

            connection.Execute(
                "UPDATE dbo.ReportBuilderBackgroundRuns SET ProgressPercent = 45 WHERE Id = @Id;",
                new { Id = runId });

            var result = RunReport(BuildRunRequest(layout));
            var resultJson = JsonSerializer.Serialize(result, ReportJsonOptions);

            connection.Execute(
                @"
UPDATE dbo.ReportBuilderBackgroundRuns
SET [Status] = N'Completed',
    ProgressPercent = 100,
    ResultJson = @ResultJson,
    [Summary] = @Summary,
    ResultRowCount = @RowCount,
    CompletedAt = SYSUTCDATETIME()
WHERE Id = @Id;",
                new
                {
                    Id = runId,
                    ResultJson = resultJson,
                    Summary = $"{result.RowCount:N0} row(s) ready for export.",
                    RowCount = result.RowCount
                });
        }
        catch (Exception ex)
        {
            connection.Execute(
                @"
UPDATE dbo.ReportBuilderBackgroundRuns
SET [Status] = N'Failed',
    ProgressPercent = 100,
    ErrorMessage = @ErrorMessage,
    [Summary] = N'Background report failed.',
    CompletedAt = SYSUTCDATETIME()
WHERE Id = @Id;",
                new { Id = runId, ErrorMessage = ex.Message });
        }
    }

    private ReportSavedReportDetailDto GetSavedReportDetail(IDbConnection connection, int id, int currentUserId, int currentRoleId, bool requireEdit, int tenantId)
    {
        var row = connection.QuerySingleOrDefault(
            @"
SELECT Id,
       [Name],
       ISNULL([Description], N'') AS [Description],
       TableKey,
       DefinitionJson,
       DefaultExportFormat,
       PreferredChartType,
       IsSensitive,
       CreatedAt,
       ModifiedAt,
       CreatedByUserId
FROM dbo.ReportBuilderSavedReports
WHERE Id = @Id
  AND IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, TenantId = tenantId });

        if (row == null)
        {
            throw new NotFoundException($"Saved report '{id}' was not found.");
        }

        var data = (IDictionary<string, object?>)row;
        var detail = new ReportSavedReportDetailDto
        {
            Id = ToInt32(data["Id"]),
            Name = Convert.ToString(data["Name"], CultureInfo.InvariantCulture) ?? string.Empty,
            Description = Convert.ToString(data["Description"], CultureInfo.InvariantCulture) ?? string.Empty,
            TableKey = Convert.ToString(data["TableKey"], CultureInfo.InvariantCulture) ?? string.Empty,
            DefaultExportFormat = NormalizeExportFormat(Convert.ToString(data["DefaultExportFormat"], CultureInfo.InvariantCulture)),
            PreferredChartType = NormalizeChartType(Convert.ToString(data["PreferredChartType"], CultureInfo.InvariantCulture)),
            IsSensitive = ToBoolean(data["IsSensitive"]),
            CreatedAt = ToDateTime(data["CreatedAt"]),
            ModifiedAt = ToNullableDateTime(data["ModifiedAt"]),
            CreatedByUserId = ToInt32(data["CreatedByUserId"]),
            Layout = NormalizeLayout(
                JsonSerializer.Deserialize<SavedReportLayoutDto>(Convert.ToString(data["DefinitionJson"], CultureInfo.InvariantCulture) ?? "{}", ReportJsonOptions)
                    ?? new SavedReportLayoutDto(),
                Convert.ToString(data["PreferredChartType"], CultureInfo.InvariantCulture))
        };

        detail.TableDisplayName = ToDisplayName(ParseTableKey(detail.TableKey).TableName);
        detail.AccessRules = GetSavedReportRoleAccessLookup(connection, [detail.Id])
            .TryGetValue(detail.Id, out var accessRules)
                ? accessRules
                : [];

        var favorites = GetFavoriteReportIds(connection, currentUserId);
        ApplySavedReportPermissions(detail, detail.AccessRules, favorites, currentUserId, currentRoleId);

        if (requireEdit && !detail.CanEdit)
        {
            throw new UnauthorizedAccessException("You do not have permission to edit this report.");
        }

        if (!requireEdit && !detail.CanView)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this report.");
        }

        return detail;
    }

    private static RunReportRequest BuildRunRequest(SavedReportLayoutDto layout)
    {
        return new RunReportRequest
        {
            TableKey = layout.TableKey,
            Joins = layout.Joins.Select(join => new ReportJoinRequest
            {
                LinkKey = join.LinkKey,
                SortOrder = join.SortOrder
            }).ToList(),
            Columns = layout.Columns.ToList(),
            GroupByColumns = layout.GroupByColumns.ToList(),
            CalculatedColumns = layout.CalculatedColumns.Select(column => new ReportCalculatedColumnRequest
            {
                Alias = column.Alias,
                Expression = column.Expression
            }).ToList(),
            Aggregates = layout.Aggregates.Select(aggregate => new ReportAggregateRequest
            {
                AggregateType = aggregate.AggregateType,
                Alias = aggregate.Alias,
                SourceIdentifier = aggregate.SourceIdentifier
            }).ToList(),
            Filters = layout.Filters.Select(filter => new ReportFilterRequest
            {
                ColumnKey = filter.ColumnKey,
                ColumnName = filter.ColumnName,
                Operator = filter.Operator,
                Value = filter.Value,
                ValueTo = filter.ValueTo
            }).ToList(),
            SortColumn = layout.SortColumn,
            SortDescending = layout.SortDescending,
            MaxRows = layout.MaxRows,
            IncludeSqlPreview = layout.IncludeSqlPreview,
            CurrencySettings = layout.CurrencySettings ?? new ReportCurrencySettingsDto()
        };
    }

    private static SavedReportLayoutDto NormalizeLayout(SavedReportLayoutDto? layout, string? chartType)
    {
        layout ??= new SavedReportLayoutDto();
        layout.ChartLayout ??= new ReportChartLayoutDto();
        layout.CurrencySettings ??= new ReportCurrencySettingsDto();
        layout.ChartLayout.ChartType = NormalizeChartType(string.IsNullOrWhiteSpace(layout.ChartLayout.ChartType) ? chartType : layout.ChartLayout.ChartType);
        layout.MaxRows = Math.Clamp(layout.MaxRows <= 0 ? 500 : layout.MaxRows, 1, 5000);
        layout.CurrencySettings.ReportingCurrencyCode = NormalizeCurrencyCode(layout.CurrencySettings.ReportingCurrencyCode) ?? "USD";
        return layout;
    }

    private static string NormalizeChartType(string? chartType)
    {
        var normalized = (chartType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "line" => "line",
            "pie" => "pie",
            "kpi" => "kpi",
            _ => "bar"
        };
    }

    private static string NormalizeExportFormat(string? format)
    {
        var normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "pdf" => "pdf",
            "csv" => "csv",
            "json" => "json",
            _ => "xls"
        };
    }

    private static List<ReportSavedReportRoleAccessDto> NormalizeAccessRules(IEnumerable<ReportSavedReportRoleAccessDto>? source, ISet<int> validRoleIds)
    {
        var accessRules = new List<ReportSavedReportRoleAccessDto>();
        foreach (var item in source ?? [])
        {
            if (item.RoleId <= 0)
            {
                continue;
            }

            if (!validRoleIds.Contains(item.RoleId))
            {
                throw new ValidationException($"Role ID '{item.RoleId}' was not found.");
            }

            var existing = accessRules.FirstOrDefault(rule => rule.RoleId == item.RoleId);
            if (existing != null)
            {
                existing.CanView = item.CanView;
                existing.CanEdit = item.CanEdit;
                existing.CanExport = item.CanExport;
                continue;
            }

            accessRules.Add(new ReportSavedReportRoleAccessDto
            {
                RoleId = item.RoleId,
                RoleName = item.RoleName,
                CanView = item.CanView,
                CanEdit = item.CanEdit,
                CanExport = item.CanExport
            });
        }

        return accessRules;
    }

    private static void ApplySavedReportPermissions(
        ReportSavedReportSummaryDto summary,
        IReadOnlyList<ReportSavedReportRoleAccessDto> accessRules,
        ISet<int> favoriteIds,
        int currentUserId,
        int currentRoleId)
    {
        var isAdmin = currentRoleId == (int)UserRole.Admin;
        var isOwner = summary.CreatedByUserId == currentUserId;
        var matchingRole = accessRules.FirstOrDefault(rule => rule.RoleId == currentRoleId);
        var hasCustomSecurity = accessRules.Count > 0;

        summary.CanView = isAdmin
                          || isOwner
                          || (matchingRole?.CanView ?? false)
                          || (!summary.IsSensitive && !hasCustomSecurity);
        summary.CanEdit = isAdmin
                          || isOwner
                          || (matchingRole?.CanEdit ?? false);
        summary.CanExport = isAdmin
                            || isOwner
                            || (matchingRole?.CanExport ?? false)
                            || (!summary.IsSensitive && !hasCustomSecurity);
        summary.IsFavorite = favoriteIds.Contains(summary.Id);
    }

    private static HashSet<int> GetFavoriteReportIds(IDbConnection connection, int currentUserId)
    {
        return connection.Query<int>(
                "SELECT ReportId FROM dbo.ReportBuilderFavoriteReports WHERE UserId = @UserId;",
                new { UserId = currentUserId })
            .ToHashSet();
    }

    private static void SetFavoriteInternal(IDbConnection connection, int reportId, int currentUserId, bool isFavorite)
    {
        if (isFavorite)
        {
            connection.Execute(
                @"
IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderFavoriteReports WHERE UserId = @UserId AND ReportId = @ReportId)
BEGIN
    INSERT INTO dbo.ReportBuilderFavoriteReports (UserId, ReportId)
    VALUES (@UserId, @ReportId);
END;",
                new { UserId = currentUserId, ReportId = reportId });
            return;
        }

        connection.Execute(
            "DELETE FROM dbo.ReportBuilderFavoriteReports WHERE UserId = @UserId AND ReportId = @ReportId;",
            new { UserId = currentUserId, ReportId = reportId });
    }

    private static Dictionary<int, List<ReportSavedReportRoleAccessDto>> GetSavedReportRoleAccessLookup(IDbConnection connection, IEnumerable<int> reportIds)
    {
        var ids = reportIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, List<ReportSavedReportRoleAccessDto>>();
        }

        var lookup = new Dictionary<int, List<ReportSavedReportRoleAccessDto>>();
        var rows = connection.Query(
            @"
SELECT ReportId,
       rr.RoleId,
       r.Name AS RoleName,
       CanView,
       CanEdit,
       CanExport
FROM dbo.ReportBuilderSavedReportRoles rr
LEFT JOIN dbo.Roles r ON r.Id = rr.RoleId
WHERE rr.ReportId IN @Ids;",
            new { Ids = ids });

        foreach (var row in rows)
        {
            var data = (IDictionary<string, object?>)row;
            var reportId = ToInt32(data["ReportId"]);
            if (!lookup.TryGetValue(reportId, out var list))
            {
                list = new List<ReportSavedReportRoleAccessDto>();
                lookup[reportId] = list;
            }

            list.Add(new ReportSavedReportRoleAccessDto
            {
                RoleId = ToInt32(data["RoleId"]),
                RoleName = Convert.ToString(data["RoleName"], CultureInfo.InvariantCulture) ?? string.Empty,
                CanView = ToBoolean(data["CanView"]),
                CanEdit = ToBoolean(data["CanEdit"]),
                CanExport = ToBoolean(data["CanExport"])
            });
        }

        return lookup;
    }

    private static HashSet<string> GetUniqueColumns(IDbConnection connection)
    {
        return connection.Query<string>(
                @"
WITH UniqueSingleColumnIndexes AS
(
    SELECT i.object_id, MAX(ic.column_id) AS ColumnId
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE (i.is_primary_key = 1 OR i.is_unique = 1)
      AND ic.is_included_column = 0
    GROUP BY i.object_id, i.index_id
    HAVING COUNT(*) = 1
       AND MAX(ic.key_ordinal) = 1
)
SELECT CONCAT(s.name, '.', t.name, '.', c.name)
FROM UniqueSingleColumnIndexes u
INNER JOIN sys.tables t ON t.object_id = u.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = u.object_id AND c.column_id = u.ColumnId
WHERE t.is_ms_shipped = 0;")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void PopulateLinkCardinality(ReportTableLinkDto link, ISet<string> uniqueColumns)
    {
        var sourceKey = $"{link.SourceTableKey}.{link.SourceColumnName}";
        var targetKey = $"{link.TargetTableKey}.{link.TargetColumnName}";
        var sourceUnique = uniqueColumns.Contains(sourceKey);
        var targetUnique = uniqueColumns.Contains(targetKey);

        link.SourceCardinality = sourceUnique ? "1" : "many";
        link.TargetCardinality = targetUnique ? "1" : "many";
        link.CardinalityLabel = $"{link.SourceCardinality} -> {link.TargetCardinality}";
        link.MayDuplicateRows = !targetUnique;
        link.DuplicationWarning = targetUnique
            ? string.Empty
            : $"Joining from {link.SourceTableDisplayName} to {link.TargetTableDisplayName} may duplicate source rows because {link.TargetColumnName} is not unique on the target table.";
    }

    private static string GetSampleValuesText(IDbConnection connection, ReportTableDto table, string columnName)
    {
        var sql = $@"
SELECT TOP (3) CONVERT(NVARCHAR(160), {Quote(columnName)}) AS SampleValue
FROM {Quote(table.SchemaName)}.{Quote(table.TableName)}
WHERE {Quote(columnName)} IS NOT NULL
GROUP BY {Quote(columnName)}
ORDER BY CONVERT(NVARCHAR(160), {Quote(columnName)});";

        var samples = connection.Query<string>(sql)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Take(3)
            .ToList();

        return samples.Count == 0 ? "No sample values yet." : string.Join("  •  ", samples);
    }

    private static (string BaseCurrencyCode, string CounterCurrencyCode) GetConfiguredCurrencyContext(IDbConnection connection, int tenantId)
    {
        var values = connection.Query(
                @"
IF OBJECT_ID('dbo.AppConstants', 'U') IS NULL
BEGIN
    SELECT CAST(NULL AS NVARCHAR(120)) AS [Key], CAST(NULL AS NVARCHAR(4000)) AS [Value]
    WHERE 1 = 0;
END
ELSE
BEGIN
    SELECT [Key], [Value]
    FROM dbo.AppConstants
    WHERE (@TenantId = 0 OR TenantId = @TenantId);
END;",
                new { TenantId = tenantId })
            .Select(row => (IDictionary<string, object?>)row)
            .Where(row => row["Key"] != null)
            .ToDictionary(
                row => Convert.ToString(row["Key"], CultureInfo.InvariantCulture) ?? string.Empty,
                row => Convert.ToString(row["Value"], CultureInfo.InvariantCulture) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        var baseCurrency = NormalizeCurrencyCode(values.TryGetValue("BaseCurrencyCode", out var configuredBase)
                ? configuredBase
                : values.TryGetValue("DefaultCurrencyCode", out var defaultBase) ? defaultBase : null)
            ?? "USD";
        var counterCurrency = NormalizeCurrencyCode(values.TryGetValue("CounterCurrencyCode", out var configuredCounter)
                ? configuredCounter
                : null)
            ?? baseCurrency;
        return (baseCurrency, counterCurrency);
    }

    private static List<string> GetAvailableCurrencyCodes(IDbConnection connection, string baseCurrencyCode, string counterCurrencyCode, int tenantId)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            baseCurrencyCode,
            counterCurrencyCode
        };

        if (TableExists(connection, "dbo", "CurrencyRates"))
        {
            foreach (var code in connection.Query<string>(
                "SELECT DISTINCT Code FROM dbo.CurrencyRates WHERE Code IS NOT NULL AND LTRIM(RTRIM(Code)) <> N'' AND (@TenantId = 0 OR TenantId = @TenantId);",
                new { TenantId = tenantId }))
            {
                var normalized = NormalizeCurrencyCode(code);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    codes.Add(normalized);
                }
            }
        }

        return codes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool TableExists(IDbConnection connection, string schemaName, string tableName)
    {
        return connection.ExecuteScalar<int>(
            @"
SELECT COUNT(1)
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @SchemaName
  AND t.name = @TableName;",
            new { SchemaName = schemaName, TableName = tableName }) > 0;
    }

    private static string? NormalizeCurrencyCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return normalized.Length == 3 ? normalized : null;
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ToBoolean(object? value)
        => value switch
        {
            bool boolean => boolean,
            byte tiny => tiny != 0,
            short small => small != 0,
            int integer => integer != 0,
            long big => big != 0,
            _ => bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed
        };

    private static int ToInt32(object? value)
        => value == null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static DateTime ToDateTime(object? value)
        => value == null ? DateTime.MinValue : Convert.ToDateTime(value, CultureInfo.InvariantCulture);

    private static DateTime? ToNullableDateTime(object? value)
        => value == null || value == DBNull.Value ? null : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
}
