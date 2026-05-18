using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Reports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed partial class ReportBuilderViewModel
    {
        private ReportJoinGraphDto? _joinGraph;
        private ReportSchemaInspectorDto? _schemaInspector;
        private ReportSavedReportSummaryDto? _selectedSavedReport;
        private BackgroundReportRunSummaryDto? _selectedBackgroundRun;
        private ReportSavedReportDetailDto? _loadedSavedReport;
        private string _savedReportName = string.Empty;
        private string _savedReportDescription = string.Empty;
        private string _selectedExportFormat = "xls";
        private string _selectedChartType = "bar";
        private string _chartStatusText = "Run a report to build charts.";
        private string? _selectedChartCategoryColumnName;
        private string? _selectedChartValueColumnName;
        private bool _savedReportIsSensitive;
        private bool _currencyModeEnabled;
        private ReportBuilderColumnOption? _selectedCurrencyAmountColumn;
        private ReportBuilderColumnOption? _selectedCurrencyCodeColumn;
        private ReportBuilderColumnOption? _selectedRateToBaseColumn;
        private ReportBuilderColumnOption? _selectedBaseCurrencyColumn;
        private ReportBuilderColumnOption? _selectedCounterCurrencyColumn;
        private ReportBuilderColumnOption? _selectedCounterRateColumn;
        private string _selectedReportingCurrencyCode = "USD";

        public ObservableCollection<ReportSavedReportSummaryDto> SavedReports { get; } = new();
        public ObservableCollection<BackgroundReportRunSummaryDto> BackgroundRuns { get; } = new();
        public ObservableCollection<ReportBuilderRoleAccessRow> ReportRoleAccess { get; } = new();
        public ObservableCollection<string> AvailableExportFormats { get; } = new();
        public ObservableCollection<string> AvailableChartTypes { get; } = new();
        public ObservableCollection<string> AvailableCurrencyCodes { get; } = new();
        public ObservableCollection<string> ResultColumnNames { get; } = new();
        public ObservableCollection<ReportBuilderChartPoint> ChartPoints { get; } = new();
        public ObservableCollection<ReportBuilderKpiCard> KpiCards { get; } = new();

        public ICommand SaveCurrentReportCommand { get; private set; } = null!;
        public ICommand LoadSavedReportCommand { get; private set; } = null!;
        public ICommand DeleteSavedReportCommand { get; private set; } = null!;
        public ICommand ToggleFavoriteReportCommand { get; private set; } = null!;
        public ICommand QueueBackgroundRunCommand { get; private set; } = null!;
        public ICommand RefreshBackgroundRunsCommand { get; private set; } = null!;
        public ICommand OpenBackgroundRunResultCommand { get; private set; } = null!;

        public ReportJoinGraphDto? JoinGraph
        {
            get => _joinGraph;
            private set
            {
                if (ReferenceEquals(_joinGraph, value)) return;
                _joinGraph = value;
                OnPropertyChanged(nameof(JoinGraph));
                OnPropertyChanged(nameof(HasJoinGraph));
            }
        }

        public ReportSchemaInspectorDto? SchemaInspector
        {
            get => _schemaInspector;
            private set
            {
                if (ReferenceEquals(_schemaInspector, value)) return;
                _schemaInspector = value;
                OnPropertyChanged(nameof(SchemaInspector));
                OnPropertyChanged(nameof(HasSchemaInspector));
                OnPropertyChanged(nameof(SchemaInspectorSummaryText));
            }
        }

        public ReportSavedReportSummaryDto? SelectedSavedReport
        {
            get => _selectedSavedReport;
            set
            {
                if (ReferenceEquals(_selectedSavedReport, value)) return;
                _selectedSavedReport = value;
                OnPropertyChanged(nameof(SelectedSavedReport));
                OnPropertyChanged(nameof(CanEditSelectedSavedReport));
                OnPropertyChanged(nameof(CanDeleteSelectedSavedReport));

                if (value == null)
                {
                    ApplySavedReportEditor(null);
                }
            }
        }

        public BackgroundReportRunSummaryDto? SelectedBackgroundRun
        {
            get => _selectedBackgroundRun;
            set
            {
                if (ReferenceEquals(_selectedBackgroundRun, value)) return;
                _selectedBackgroundRun = value;
                OnPropertyChanged(nameof(SelectedBackgroundRun));
                OnPropertyChanged(nameof(CanOpenSelectedBackgroundRun));
            }
        }

        public string SavedReportName
        {
            get => _savedReportName;
            set
            {
                if (_savedReportName == value) return;
                _savedReportName = value;
                OnPropertyChanged(nameof(SavedReportName));
            }
        }

        public string SavedReportDescription
        {
            get => _savedReportDescription;
            set
            {
                if (_savedReportDescription == value) return;
                _savedReportDescription = value;
                OnPropertyChanged(nameof(SavedReportDescription));
            }
        }

        public bool SavedReportIsSensitive
        {
            get => _savedReportIsSensitive;
            set
            {
                if (_savedReportIsSensitive == value) return;
                _savedReportIsSensitive = value;
                OnPropertyChanged(nameof(SavedReportIsSensitive));
            }
        }

        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set
            {
                var normalized = NormalizeExportFormat(value);
                if (_selectedExportFormat == normalized) return;
                _selectedExportFormat = normalized;
                OnPropertyChanged(nameof(SelectedExportFormat));
            }
        }

        public string SelectedChartType
        {
            get => _selectedChartType;
            set
            {
                var normalized = NormalizeChartType(value);
                if (_selectedChartType == normalized) return;
                _selectedChartType = normalized;
                OnPropertyChanged(nameof(SelectedChartType));
                RebuildCharts();
            }
        }

        public string? SelectedChartCategoryColumnName
        {
            get => _selectedChartCategoryColumnName;
            set
            {
                if (_selectedChartCategoryColumnName == value) return;
                _selectedChartCategoryColumnName = value;
                OnPropertyChanged(nameof(SelectedChartCategoryColumnName));
                RebuildCharts();
            }
        }

        public string? SelectedChartValueColumnName
        {
            get => _selectedChartValueColumnName;
            set
            {
                if (_selectedChartValueColumnName == value) return;
                _selectedChartValueColumnName = value;
                OnPropertyChanged(nameof(SelectedChartValueColumnName));
                RebuildCharts();
            }
        }

        public string ChartStatusText
        {
            get => _chartStatusText;
            private set
            {
                if (_chartStatusText == value) return;
                _chartStatusText = value;
                OnPropertyChanged(nameof(ChartStatusText));
            }
        }

        public bool CurrencyModeEnabled
        {
            get => _currencyModeEnabled;
            set
            {
                if (_currencyModeEnabled == value) return;
                _currencyModeEnabled = value;
                OnPropertyChanged(nameof(CurrencyModeEnabled));
            }
        }

        public ReportBuilderColumnOption? SelectedCurrencyAmountColumn
        {
            get => _selectedCurrencyAmountColumn;
            set
            {
                if (ReferenceEquals(_selectedCurrencyAmountColumn, value)) return;
                _selectedCurrencyAmountColumn = value;
                OnPropertyChanged(nameof(SelectedCurrencyAmountColumn));
            }
        }

        public ReportBuilderColumnOption? SelectedCurrencyCodeColumn
        {
            get => _selectedCurrencyCodeColumn;
            set
            {
                if (ReferenceEquals(_selectedCurrencyCodeColumn, value)) return;
                _selectedCurrencyCodeColumn = value;
                OnPropertyChanged(nameof(SelectedCurrencyCodeColumn));
            }
        }

        public ReportBuilderColumnOption? SelectedRateToBaseColumn
        {
            get => _selectedRateToBaseColumn;
            set
            {
                if (ReferenceEquals(_selectedRateToBaseColumn, value)) return;
                _selectedRateToBaseColumn = value;
                OnPropertyChanged(nameof(SelectedRateToBaseColumn));
            }
        }

        public ReportBuilderColumnOption? SelectedBaseCurrencyColumn
        {
            get => _selectedBaseCurrencyColumn;
            set
            {
                if (ReferenceEquals(_selectedBaseCurrencyColumn, value)) return;
                _selectedBaseCurrencyColumn = value;
                OnPropertyChanged(nameof(SelectedBaseCurrencyColumn));
            }
        }

        public ReportBuilderColumnOption? SelectedCounterCurrencyColumn
        {
            get => _selectedCounterCurrencyColumn;
            set
            {
                if (ReferenceEquals(_selectedCounterCurrencyColumn, value)) return;
                _selectedCounterCurrencyColumn = value;
                OnPropertyChanged(nameof(SelectedCounterCurrencyColumn));
            }
        }

        public ReportBuilderColumnOption? SelectedCounterRateColumn
        {
            get => _selectedCounterRateColumn;
            set
            {
                if (ReferenceEquals(_selectedCounterRateColumn, value)) return;
                _selectedCounterRateColumn = value;
                OnPropertyChanged(nameof(SelectedCounterRateColumn));
            }
        }

        public string SelectedReportingCurrencyCode
        {
            get => _selectedReportingCurrencyCode;
            set
            {
                var normalized = string.IsNullOrWhiteSpace(value) ? "USD" : value.Trim().ToUpperInvariant();
                if (_selectedReportingCurrencyCode == normalized) return;
                _selectedReportingCurrencyCode = normalized;
                OnPropertyChanged(nameof(SelectedReportingCurrencyCode));
            }
        }

        public bool HasJoinGraph => JoinGraph != null && JoinGraph.Nodes.Count > 0;
        public bool HasSchemaInspector => SchemaInspector != null;
        public string SchemaInspectorSummaryText => SchemaInspector == null
            ? "Choose a table to inspect its schema."
            : $"{SchemaInspector.Columns.Count:N0} column(s) • {SchemaInspector.Relations.Count:N0} relation(s) • approx. {SchemaInspector.EstimatedRowCount:N0} row(s)";
        public bool CanEditSelectedSavedReport => SelectedSavedReport?.CanEdit ?? false;
        public bool CanDeleteSelectedSavedReport => SelectedSavedReport?.CanEdit ?? false;
        public bool CanOpenSelectedBackgroundRun => SelectedBackgroundRun?.ResultAvailable == true;
        public bool HasChartData => ChartPoints.Count > 0 || KpiCards.Count > 0;
        public bool CanExportCurrentResult => HasResult && (_loadedSavedReport?.CanExport ?? true);

        private void InitializeProFeatures()
        {
            AvailableExportFormats.Add("xls");
            AvailableExportFormats.Add("pdf");
            AvailableExportFormats.Add("csv");
            AvailableExportFormats.Add("json");

            AvailableChartTypes.Add("bar");
            AvailableChartTypes.Add("line");
            AvailableChartTypes.Add("pie");
            AvailableChartTypes.Add("kpi");
        }

        private void InitializeProCommands()
        {
            SaveCurrentReportCommand = new RelayCommand(_ => SaveCurrentReportAsync().SafeFireAndForget(HandleBackgroundException));
            LoadSavedReportCommand = new RelayCommand(_ => LoadSelectedSavedReportAsync().SafeFireAndForget(HandleBackgroundException));
            DeleteSavedReportCommand = new RelayCommand(_ => DeleteSelectedSavedReportAsync().SafeFireAndForget(HandleBackgroundException));
            ToggleFavoriteReportCommand = new RelayCommand(_ => ToggleFavoriteReportAsync().SafeFireAndForget(HandleBackgroundException));
            QueueBackgroundRunCommand = new RelayCommand(_ => QueueBackgroundRunAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshBackgroundRunsCommand = new RelayCommand(_ => RefreshBackgroundRunsAsync().SafeFireAndForget(HandleBackgroundException));
            OpenBackgroundRunResultCommand = new RelayCommand(_ => OpenSelectedBackgroundRunResultAsync().SafeFireAndForget(HandleBackgroundException));
        }

        private async Task LoadProMetadataAsync()
        {
            var securityOptions = await _api.GetSecurityOptionsAsync();
            AvailableCurrencyCodes.Clear();
            foreach (var code in securityOptions.CurrencyCodes)
            {
                AvailableCurrencyCodes.Add(code);
            }

            if (AvailableCurrencyCodes.Count == 0)
            {
                AvailableCurrencyCodes.Add(securityOptions.DefaultBaseCurrencyCode);
                if (!string.Equals(securityOptions.DefaultCounterCurrencyCode, securityOptions.DefaultBaseCurrencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    AvailableCurrencyCodes.Add(securityOptions.DefaultCounterCurrencyCode);
                }
            }

            if (string.IsNullOrWhiteSpace(SelectedReportingCurrencyCode))
            {
                SelectedReportingCurrencyCode = securityOptions.DefaultBaseCurrencyCode;
            }

            if (ReportRoleAccess.Count == 0)
            {
                ReportRoleAccess.Clear();
                foreach (var roleName in securityOptions.RoleNames)
                {
                    ReportRoleAccess.Add(new ReportBuilderRoleAccessRow
                    {
                        RoleName = roleName,
                        CanView = string.Equals(roleName, securityOptions.CurrentRoleName, StringComparison.OrdinalIgnoreCase),
                        CanEdit = string.Equals(roleName, securityOptions.CurrentRoleName, StringComparison.OrdinalIgnoreCase),
                        CanExport = string.Equals(roleName, securityOptions.CurrentRoleName, StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            JoinGraph = await _api.GetJoinGraphAsync();
            await RefreshSavedReportsAsync();
            await RefreshBackgroundRunsAsync();
        }

        private async Task RefreshSavedReportsAsync()
        {
            var savedReports = await _api.GetSavedReportsAsync();
            var selectedId = SelectedSavedReport?.Id;
            SavedReports.Clear();
            foreach (var report in savedReports)
            {
                SavedReports.Add(report);
            }

            SelectedSavedReport = SavedReports.FirstOrDefault(report => report.Id == selectedId)
                ?? SavedReports.FirstOrDefault();
        }

        private async Task RefreshBackgroundRunsAsync()
        {
            var runs = await _api.GetBackgroundRunsAsync();
            var selectedId = SelectedBackgroundRun?.Id;
            BackgroundRuns.Clear();
            foreach (var run in runs)
            {
                BackgroundRuns.Add(run);
            }

            SelectedBackgroundRun = BackgroundRuns.FirstOrDefault(run => run.Id == selectedId)
                ?? BackgroundRuns.FirstOrDefault();
        }

        private async Task RefreshSchemaInspectorAsync()
        {
            if (SelectedTable == null)
            {
                SchemaInspector = null;
                return;
            }

            SchemaInspector = await _api.GetSchemaInspectorAsync(SelectedTable.Key);
        }

        private async Task SaveCurrentReportAsync()
        {
            var layout = BuildCurrentLayout();
            if (string.IsNullOrWhiteSpace(layout.TableKey))
            {
                SetStatus("Choose a table before saving the report layout.", false);
                return;
            }

            var request = new SaveReportDefinitionRequest
            {
                Id = _loadedSavedReport?.Id,
                Name = string.IsNullOrWhiteSpace(SavedReportName) ? (SelectedTable?.DisplayName ?? "Report") : SavedReportName.Trim(),
                Description = SavedReportDescription?.Trim() ?? string.Empty,
                Layout = layout,
                IsFavorite = _loadedSavedReport?.IsFavorite ?? false,
                IsSensitive = SavedReportIsSensitive,
                DefaultExportFormat = SelectedExportFormat,
                PreferredChartType = SelectedChartType,
                AccessRules = ReportRoleAccess
                    .Where(row => row.CanView || row.CanEdit || row.CanExport)
                    .Select(row => new ReportSavedReportRoleAccessDto
                    {
                        RoleName = row.RoleName,
                        CanView = row.CanView,
                        CanEdit = row.CanEdit,
                        CanExport = row.CanExport
                    })
                    .ToList()
            };

            var detail = await _api.SaveReportAsync(request);
            _loadedSavedReport = detail;
            ApplySavedReportEditor(detail);
            await RefreshSavedReportsAsync();
            SelectedSavedReport = SavedReports.FirstOrDefault(report => report.Id == detail.Id);
            OnPropertyChanged(nameof(CanExportCurrentResult));
            SetStatus($"Saved report '{detail.Name}'.", true);
        }

        private async Task LoadSelectedSavedReportAsync()
        {
            if (SelectedSavedReport == null)
            {
                SetStatus("Choose a saved report first.", false);
                return;
            }

            var detail = await _api.GetSavedReportAsync(SelectedSavedReport.Id);
            await ApplySavedReportDetailAsync(detail);
            SetStatus($"Loaded saved report '{detail.Name}'.", true);
        }

        private async Task DeleteSelectedSavedReportAsync()
        {
            if (SelectedSavedReport == null)
            {
                SetStatus("Choose a saved report first.", false);
                return;
            }

            await _api.DeleteSavedReportAsync(SelectedSavedReport.Id);
            if (_loadedSavedReport?.Id == SelectedSavedReport.Id)
            {
                _loadedSavedReport = null;
                OnPropertyChanged(nameof(CanExportCurrentResult));
            }

            await RefreshSavedReportsAsync();
            SetStatus("Saved report deleted.", true);
        }

        private async Task ToggleFavoriteReportAsync()
        {
            if (SelectedSavedReport == null)
            {
                SetStatus("Choose a saved report first.", false);
                return;
            }

            var updated = await _api.SetFavoriteAsync(SelectedSavedReport.Id, !SelectedSavedReport.IsFavorite);
            if (_loadedSavedReport?.Id == updated.Id)
            {
                _loadedSavedReport.IsFavorite = updated.IsFavorite;
            }

            await RefreshSavedReportsAsync();
            SelectedSavedReport = SavedReports.FirstOrDefault(report => report.Id == updated.Id);
            SetStatus(updated.IsFavorite ? "Report added to favorites." : "Report removed from favorites.", true);
        }

        private async Task QueueBackgroundRunAsync()
        {
            var layout = BuildCurrentLayout();
            if (string.IsNullOrWhiteSpace(layout.TableKey))
            {
                SetStatus("Choose a table before queueing a background report.", false);
                return;
            }

            await _api.QueueBackgroundRunAsync(new QueueBackgroundReportRunRequest
            {
                ReportName = string.IsNullOrWhiteSpace(SavedReportName) ? (SelectedTable?.DisplayName ?? "Report") : SavedReportName.Trim(),
                Layout = layout
            });

            await RefreshBackgroundRunsAsync();
            SetStatus("Background report queued.", true);
        }

        private async Task OpenSelectedBackgroundRunResultAsync()
        {
            if (SelectedBackgroundRun == null || !SelectedBackgroundRun.ResultAvailable)
            {
                SetStatus("Choose a completed background report first.", false);
                return;
            }

            var result = await _api.GetBackgroundRunResultAsync(SelectedBackgroundRun.Id);
            ResultView = BuildResultView(result);
            ResultTableName = SelectedBackgroundRun.ReportName;
            _lastResultRunAt = DateTime.Now;
            ResultSummary = $"{result.RowCount:N0} row(s), {result.Columns.Count:N0} column(s), background result.";
            ApplyAdvancedResultState(BuildRunRequestFromLayout(BuildCurrentLayout()), result);
            _loadedSavedReport = null;
            UpdateResultColumnsForCharts();
            RebuildCharts();
            OnPropertyChanged(nameof(LastRunText));
            OnPropertyChanged(nameof(CanExportCurrentResult));
            SetStatus($"Background report '{SelectedBackgroundRun.ReportName}' loaded.", true);
        }

        private SavedReportLayoutDto BuildCurrentLayout()
        {
            return new SavedReportLayoutDto
            {
                TableKey = SelectedTable?.Key,
                Joins = SelectedJoins.Select((link, index) => new ReportJoinRequest
                {
                    LinkKey = link.LinkKey,
                    SortOrder = index
                }).ToList(),
                Columns = Columns.Where(column => column.IsSelected).Select(column => column.Key).ToList(),
                GroupByColumns = GroupByColumns
                    .Where(row => row.SelectedColumn != null)
                    .Select(row => row.SelectedColumn!.Key)
                    .ToList(),
                CalculatedColumns = CalculatedFields
                    .Where(row => !string.IsNullOrWhiteSpace(row.Expression))
                    .Select(row => new ReportCalculatedColumnRequest
                    {
                        Alias = row.Alias?.Trim() ?? string.Empty,
                        Expression = row.Expression?.Trim() ?? string.Empty
                    }).ToList(),
                Aggregates = AggregateRows
                    .Where(row => row.SelectedAggregateType != null)
                    .Where(row => !row.NeedsSourceColumn || row.SelectedColumn != null)
                    .Select(row => new ReportAggregateRequest
                    {
                        AggregateType = row.SelectedAggregateType!.Key,
                        Alias = row.Alias?.Trim() ?? string.Empty,
                        SourceIdentifier = row.NeedsSourceColumn ? row.SelectedColumn?.Key : null
                    }).ToList(),
                Filters = Filters
                    .Where(filter => filter.SelectedColumn != null && filter.SelectedOperator != null)
                    .Select(filter => new ReportFilterRequest
                    {
                        ColumnKey = filter.SelectedColumn!.Key,
                        ColumnName = filter.SelectedColumn.ColumnName,
                        Operator = filter.SelectedOperator!.Key,
                        Value = filter.Value,
                        ValueTo = filter.ValueTo
                    }).ToList(),
                SortColumn = SelectedSortColumn?.Key,
                SortDescending = SortDescending,
                MaxRows = MaxRows,
                IncludeSqlPreview = true,
                ChartLayout = new ReportChartLayoutDto
                {
                    ChartType = SelectedChartType,
                    CategoryColumnKey = SelectedChartCategoryColumnName,
                    ValueColumnKey = SelectedChartValueColumnName
                },
                CurrencySettings = BuildCurrencySettings()
            };
        }

        private ReportCurrencySettingsDto BuildCurrencySettings()
        {
            return new ReportCurrencySettingsDto
            {
                IsEnabled = CurrencyModeEnabled,
                AmountColumnKey = SelectedCurrencyAmountColumn?.Key,
                CurrencyCodeColumnKey = SelectedCurrencyCodeColumn?.Key,
                RateToBaseColumnKey = SelectedRateToBaseColumn?.Key,
                BaseCurrencyCodeColumnKey = SelectedBaseCurrencyColumn?.Key,
                CounterCurrencyCodeColumnKey = SelectedCounterCurrencyColumn?.Key,
                CounterRateToBaseColumnKey = SelectedCounterRateColumn?.Key,
                ReportingCurrencyCode = SelectedReportingCurrencyCode
            };
        }

        private RunReportRequest BuildRunRequestFromLayout(SavedReportLayoutDto layout)
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

        private async Task ApplySavedReportDetailAsync(ReportSavedReportDetailDto detail)
        {
            _loadedSavedReport = detail;
            ApplySavedReportEditor(detail);

            var targetTable = Tables.FirstOrDefault(table => string.Equals(table.Key, detail.Layout.TableKey, StringComparison.OrdinalIgnoreCase));
            if (targetTable == null)
            {
                await LoadMetadataAsync();
                targetTable = Tables.FirstOrDefault(table => string.Equals(table.Key, detail.Layout.TableKey, StringComparison.OrdinalIgnoreCase));
            }

            if (targetTable == null)
            {
                SetStatus($"Base table '{detail.Layout.TableKey}' was not found.", false);
                return;
            }

            _suppressSelectedTableLoad = true;
            SelectedTable = targetTable;
            _suppressSelectedTableLoad = false;
            await LoadSelectedTableDesignAsync(manageLoading: false);

            SelectedJoins.Clear();
            foreach (var join in detail.Layout.Joins.OrderBy(join => join.SortOrder))
            {
                var link = _allLinks.FirstOrDefault(candidate => string.Equals(candidate.LinkKey, join.LinkKey, StringComparison.OrdinalIgnoreCase));
                if (link != null)
                {
                    SelectedJoins.Add(link);
                }
            }

            NormalizeSelectedJoins();
            foreach (var tableKey in GetActiveTableKeys())
            {
                await EnsureTableColumnsLoadedAsync(tableKey);
            }

            RebuildColumns();
            RefreshAvailableLinks();

            var selectedColumnKeys = detail.Layout.Columns.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var column in Columns)
            {
                column.IsSelected = selectedColumnKeys.Contains(column.Key);
            }

            Filters.Clear();
            foreach (var filter in detail.Layout.Filters)
            {
                Filters.Add(new ReportBuilderFilterRow
                {
                    SelectedColumn = Columns.FirstOrDefault(column => string.Equals(column.Key, filter.ColumnKey, StringComparison.OrdinalIgnoreCase))
                        ?? Columns.FirstOrDefault(column => string.Equals(column.ColumnName, filter.ColumnName, StringComparison.OrdinalIgnoreCase))
                        ?? Columns.FirstOrDefault(),
                    SelectedOperator = OperatorOptions.FirstOrDefault(option => string.Equals(option.Key, filter.Operator, StringComparison.OrdinalIgnoreCase))
                        ?? OperatorOptions.FirstOrDefault(),
                    Value = filter.Value ?? string.Empty,
                    ValueTo = filter.ValueTo ?? string.Empty
                });
            }

            ResetAdvancedDesign();
            foreach (var calculated in detail.Layout.CalculatedColumns)
            {
                CalculatedFields.Add(new ReportBuilderCalculatedFieldRow
                {
                    Alias = calculated.Alias,
                    Expression = calculated.Expression
                });
            }

            foreach (var groupBy in detail.Layout.GroupByColumns)
            {
                GroupByColumns.Add(new ReportBuilderGroupByRow
                {
                    SelectedColumn = Columns.FirstOrDefault(column => string.Equals(column.Key, groupBy, StringComparison.OrdinalIgnoreCase))
                        ?? Columns.FirstOrDefault()
                });
            }

            foreach (var aggregate in detail.Layout.Aggregates)
            {
                var aggregateType = AggregateTypeOptions.FirstOrDefault(option => string.Equals(option.Key, aggregate.AggregateType, StringComparison.OrdinalIgnoreCase))
                    ?? AggregateTypeOptions.FirstOrDefault();
                AggregateRows.Add(new ReportBuilderAggregateRow
                {
                    SelectedAggregateType = aggregateType,
                    SelectedColumn = string.IsNullOrWhiteSpace(aggregate.SourceIdentifier)
                        ? null
                        : Columns.FirstOrDefault(column => string.Equals(column.Key, aggregate.SourceIdentifier, StringComparison.OrdinalIgnoreCase))
                            ?? Columns.FirstOrDefault(),
                    Alias = aggregate.Alias ?? string.Empty
                });
            }

            SelectedSortColumn = string.IsNullOrWhiteSpace(detail.Layout.SortColumn)
                ? Columns.FirstOrDefault()
                : Columns.FirstOrDefault(column => string.Equals(column.Key, detail.Layout.SortColumn, StringComparison.OrdinalIgnoreCase))
                    ?? Columns.FirstOrDefault();
            SortDescending = detail.Layout.SortDescending;
            MaxRows = detail.Layout.MaxRows;

            ApplyCurrencySettings(detail.Layout.CurrencySettings);

            SelectedChartType = NormalizeChartType(detail.PreferredChartType);
            SelectedExportFormat = detail.DefaultExportFormat;
            SelectedChartCategoryColumnName = detail.Layout.ChartLayout?.CategoryColumnKey;
            SelectedChartValueColumnName = detail.Layout.ChartLayout?.ValueColumnKey;

            await RefreshSchemaInspectorAsync();
            OnPropertyChanged(nameof(CanExportCurrentResult));
        }

        private void ApplySavedReportEditor(ReportSavedReportDetailDto? detail)
        {
            SavedReportName = detail?.Name ?? (SelectedTable?.DisplayName ?? string.Empty);
            SavedReportDescription = detail?.Description ?? string.Empty;
            SavedReportIsSensitive = detail?.IsSensitive ?? false;
            SelectedExportFormat = detail?.DefaultExportFormat ?? "xls";
            SelectedChartType = NormalizeChartType(detail?.PreferredChartType);

            var savedAccess = detail?.AccessRules?.ToDictionary(rule => rule.RoleName, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, ReportSavedReportRoleAccessDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in ReportRoleAccess)
            {
                if (savedAccess.TryGetValue(row.RoleName, out var accessRule))
                {
                    row.CanView = accessRule.CanView;
                    row.CanEdit = accessRule.CanEdit;
                    row.CanExport = accessRule.CanExport;
                }
                else
                {
                    row.CanView = false;
                    row.CanEdit = false;
                    row.CanExport = false;
                }
            }
        }

        private void ApplyCurrencySettings(ReportCurrencySettingsDto? settings)
        {
            settings ??= new ReportCurrencySettingsDto();
            CurrencyModeEnabled = settings.IsEnabled;
            SelectedCurrencyAmountColumn = FindColumnByKey(settings.AmountColumnKey);
            SelectedCurrencyCodeColumn = FindColumnByKey(settings.CurrencyCodeColumnKey);
            SelectedRateToBaseColumn = FindColumnByKey(settings.RateToBaseColumnKey);
            SelectedBaseCurrencyColumn = FindColumnByKey(settings.BaseCurrencyCodeColumnKey);
            SelectedCounterCurrencyColumn = FindColumnByKey(settings.CounterCurrencyCodeColumnKey);
            SelectedCounterRateColumn = FindColumnByKey(settings.CounterRateToBaseColumnKey);
            SelectedReportingCurrencyCode = string.IsNullOrWhiteSpace(settings.ReportingCurrencyCode)
                ? (AvailableCurrencyCodes.FirstOrDefault() ?? "USD")
                : settings.ReportingCurrencyCode.Trim().ToUpperInvariant();
        }

        private ReportBuilderColumnOption? FindColumnByKey(string? key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? null
                : Columns.FirstOrDefault(column => string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private void RebindCurrencyColumnSelections()
        {
            SelectedCurrencyAmountColumn = FindColumnByKey(SelectedCurrencyAmountColumn?.Key);
            SelectedCurrencyCodeColumn = FindColumnByKey(SelectedCurrencyCodeColumn?.Key);
            SelectedRateToBaseColumn = FindColumnByKey(SelectedRateToBaseColumn?.Key);
            SelectedBaseCurrencyColumn = FindColumnByKey(SelectedBaseCurrencyColumn?.Key);
            SelectedCounterCurrencyColumn = FindColumnByKey(SelectedCounterCurrencyColumn?.Key);
            SelectedCounterRateColumn = FindColumnByKey(SelectedCounterRateColumn?.Key);
        }

        private void UpdateResultColumnsForCharts()
        {
            ResultColumnNames.Clear();
            if (ResultView?.Table == null)
            {
                SelectedChartCategoryColumnName = null;
                SelectedChartValueColumnName = null;
                return;
            }

            foreach (DataColumn column in ResultView.Table.Columns)
            {
                ResultColumnNames.Add(column.ColumnName);
            }

            if (ResultColumnNames.Count == 0)
            {
                SelectedChartCategoryColumnName = null;
                SelectedChartValueColumnName = null;
                return;
            }

            var firstNumeric = ResultColumnNames.FirstOrDefault(columnName => ResultView.Table.AsEnumerable().Any(row => TryReadDecimal(row[columnName], out _)));
            var firstLabel = ResultColumnNames.FirstOrDefault(columnName => !string.Equals(columnName, firstNumeric, StringComparison.OrdinalIgnoreCase))
                ?? ResultColumnNames.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(SelectedChartCategoryColumnName) || !ResultColumnNames.Contains(SelectedChartCategoryColumnName))
            {
                _selectedChartCategoryColumnName = firstLabel;
                OnPropertyChanged(nameof(SelectedChartCategoryColumnName));
            }

            if (string.IsNullOrWhiteSpace(SelectedChartValueColumnName) || !ResultColumnNames.Contains(SelectedChartValueColumnName))
            {
                _selectedChartValueColumnName = firstNumeric;
                OnPropertyChanged(nameof(SelectedChartValueColumnName));
            }
        }

        private void RebuildCharts()
        {
            ChartPoints.Clear();
            KpiCards.Clear();

            if (ResultView?.Table == null || ResultView.Count == 0)
            {
                ChartStatusText = "Run a report to build charts.";
                OnPropertyChanged(nameof(HasChartData));
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedChartValueColumnName) || !ResultView.Table.Columns.Contains(SelectedChartValueColumnName))
            {
                ChartStatusText = "Choose a numeric value column for charts.";
                OnPropertyChanged(nameof(HasChartData));
                return;
            }

            var labelColumnName = !string.IsNullOrWhiteSpace(SelectedChartCategoryColumnName) && ResultView.Table.Columns.Contains(SelectedChartCategoryColumnName)
                ? SelectedChartCategoryColumnName
                : ResultView.Table.Columns[0].ColumnName;

            var rows = ResultView.Cast<DataRowView>().ToList();
            var points = new List<ReportBuilderChartPoint>();
            foreach (var rowView in rows)
            {
                if (!TryReadDecimal(rowView.Row[SelectedChartValueColumnName], out var value))
                {
                    continue;
                }

                points.Add(new ReportBuilderChartPoint
                {
                    Label = Convert.ToString(rowView.Row[labelColumnName], CultureInfo.CurrentCulture) ?? "(blank)",
                    Value = value
                });
            }

            if (points.Count == 0)
            {
                ChartStatusText = "The selected value column does not contain numeric values.";
                OnPropertyChanged(nameof(HasChartData));
                return;
            }

            var total = points.Sum(point => point.Value);
            foreach (var point in points.OrderByDescending(point => point.Value).Take(12))
            {
                point.Percentage = total == 0 ? 0 : (double)(point.Value / total);
                ChartPoints.Add(point);
            }

            BuildKpiCards(points);
            ChartStatusText = $"{ChartPoints.Count:N0} chart point(s) ready in {SelectedChartType.ToUpperInvariant()} mode.";
            OnPropertyChanged(nameof(HasChartData));
        }

        private void BuildKpiCards(IReadOnlyList<ReportBuilderChartPoint> points)
        {
            if (points.Count == 0)
            {
                return;
            }

            KpiCards.Add(new ReportBuilderKpiCard
            {
                Label = "Rows",
                Value = points.Count.ToString("N0", CultureInfo.CurrentCulture),
                Hint = "Rows contributing to the chart."
            });
            KpiCards.Add(new ReportBuilderKpiCard
            {
                Label = "Total",
                Value = points.Sum(point => point.Value).ToString("N2", CultureInfo.CurrentCulture),
                Hint = "Sum of the selected value column."
            });
            KpiCards.Add(new ReportBuilderKpiCard
            {
                Label = "Average",
                Value = points.Average(point => point.Value).ToString("N2", CultureInfo.CurrentCulture),
                Hint = "Average of the selected value column."
            });
            KpiCards.Add(new ReportBuilderKpiCard
            {
                Label = "Top Item",
                Value = points.OrderByDescending(point => point.Value).First().Label,
                Hint = "Highest value in the current chart set."
            });
        }

        private static bool TryReadDecimal(object? value, out decimal parsed)
        {
            switch (value)
            {
                case null:
                case DBNull:
                    parsed = 0;
                    return false;
                case decimal decimalValue:
                    parsed = decimalValue;
                    return true;
                case double doubleValue:
                    parsed = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                    return true;
                case float floatValue:
                    parsed = Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
                    return true;
                case byte byteValue:
                    parsed = byteValue;
                    return true;
                case short shortValue:
                    parsed = shortValue;
                    return true;
                case int intValue:
                    parsed = intValue;
                    return true;
                case long longValue:
                    parsed = longValue;
                    return true;
                default:
                    return decimal.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), NumberStyles.Any, CultureInfo.CurrentCulture, out parsed)
                           || decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed);
            }
        }

        private static string NormalizeChartType(string? chartType)
        {
            return (chartType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "line" => "line",
                "pie" => "pie",
                "kpi" => "kpi",
                _ => "bar"
            };
        }
    }
}
