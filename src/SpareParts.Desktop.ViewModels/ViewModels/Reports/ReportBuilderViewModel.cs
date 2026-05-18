using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Reports;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed partial class ReportBuilderViewModel : INotifyPropertyChanged
    {
        private readonly IReportBuilderApiClient _api;
        private readonly Dictionary<string, List<ReportColumnDto>> _columnCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ReportTableLinkDto> _allLinks = new();

        private ReportTableDto? _selectedTable;
        private ReportBuilderColumnOption? _selectedSortColumn;
        private DataView? _resultView;
        private int _maxRows = 500;
        private bool _sortDescending;
        private bool _isLoading;
        private bool _isDesignerPopupOpen;
        private bool _suppressSelectedTableLoad;
        private bool _suppressDesignerSelectionChange;
        private string _status = "Choose a table and build a report.";
        private string _resultSummary = "No report has been run yet.";
        private string _resultTableName = "No report selected.";
        private DateTime? _lastResultRunAt;
        private Brush _statusBrush = Brushes.LightGreen;
        private ReportTableDto? _selectedDesignerSourceTable;
        private ReportTableDto? _selectedDesignerTargetTable;
        private ReportColumnDto? _selectedDesignerSourceColumn;
        private ReportColumnDto? _selectedDesignerTargetColumn;
        private ReportBuilderJoinTypeOption? _selectedJoinType;
        private ReportTableLinkDto? _selectedCustomLink;
        private string _designerLinkName = string.Empty;
        private string _designerStatus = "Create a custom table link or use database foreign keys.";
        private Brush _designerStatusBrush = Brushes.LightGreen;

        public ReportBuilderViewModel(IReportBuilderApiClient api)
        {
            _api = api;

            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "equals", DisplayName = "Equals" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "not_equals", DisplayName = "Not equals" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "contains", DisplayName = "Contains" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "starts_with", DisplayName = "Starts with" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "ends_with", DisplayName = "Ends with" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "greater_than", DisplayName = "Greater than" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "greater_or_equal", DisplayName = "Greater or equal" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "less_than", DisplayName = "Less than" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "less_or_equal", DisplayName = "Less or equal" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "between", DisplayName = "Between" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "is_null", DisplayName = "Is empty" });
            OperatorOptions.Add(new ReportBuilderOperatorOption { Key = "is_not_null", DisplayName = "Is not empty" });

            JoinTypeOptions.Add(new ReportBuilderJoinTypeOption { Key = "LEFT", DisplayName = "Left join" });
            JoinTypeOptions.Add(new ReportBuilderJoinTypeOption { Key = "INNER", DisplayName = "Inner join" });
            _selectedJoinType = JoinTypeOptions.FirstOrDefault();
            InitializeAdvancedFeatures();
            InitializeProFeatures();

            LoadMetadataCommand = new RelayCommand(_ => LoadMetadataAsync().SafeFireAndForget(HandleBackgroundException));
            RunReportCommand = new RelayCommand(_ => RunReportAsync().SafeFireAndForget(HandleBackgroundException));
            SelectAllColumnsCommand = new RelayCommand(_ => SetAllColumns(true));
            ClearColumnsCommand = new RelayCommand(_ => SetAllColumns(false));
            AddFilterCommand = new RelayCommand(_ => AddFilter());
            RemoveFilterCommand = new RelayCommand(parameter =>
            {
                if (parameter is ReportBuilderFilterRow row)
                {
                    Filters.Remove(row);
                }
            });
            AddJoinCommand = new RelayCommand(parameter =>
            {
                if (parameter is ReportTableLinkDto link)
                {
                    AddJoinAsync(link).SafeFireAndForget(HandleBackgroundException);
                }
            });
            RemoveJoinCommand = new RelayCommand(parameter =>
            {
                if (parameter is ReportTableLinkDto link)
                {
                    RemoveJoinAsync(link).SafeFireAndForget(HandleBackgroundException);
                }
            });
            StartNewLinkCommand = new RelayCommand(_ => BeginNewLink());
            OpenDesignerPopupCommand = new RelayCommand(_ => OpenDesignerPopup());
            CloseDesignerPopupCommand = new RelayCommand(_ => IsDesignerPopupOpen = false);
            EditLinkCommand = new RelayCommand(parameter =>
            {
                EditLinkAsync(parameter as ReportTableLinkDto ?? SelectedCustomLink).SafeFireAndForget(HandleBackgroundException);
            });
            SaveLinkCommand = new RelayCommand(_ => SaveLinkAsync().SafeFireAndForget(HandleBackgroundException));
            DeleteLinkCommand = new RelayCommand(_ => DeleteSelectedLinkAsync().SafeFireAndForget(HandleBackgroundException));
            ExportResultsCommand = new RelayCommand(parameter => ExportResults(parameter as string));
            InitializeAdvancedCommands();
            InitializeProCommands();
        }

        public ObservableCollection<ReportTableDto> Tables { get; } = new();
        public ObservableCollection<ReportBuilderColumnOption> Columns { get; } = new();
        public ObservableCollection<ReportBuilderFilterRow> Filters { get; } = new();
        public ObservableCollection<ReportBuilderOperatorOption> OperatorOptions { get; } = new();
        public ObservableCollection<ReportTableLinkDto> AvailableLinks { get; } = new();
        public ObservableCollection<ReportTableLinkDto> SelectedJoins { get; } = new();
        public ObservableCollection<ReportTableLinkDto> CustomLinks { get; } = new();
        public ObservableCollection<ReportColumnDto> DesignerSourceColumns { get; } = new();
        public ObservableCollection<ReportColumnDto> DesignerTargetColumns { get; } = new();
        public ObservableCollection<ReportBuilderJoinTypeOption> JoinTypeOptions { get; } = new();

        public ICommand LoadMetadataCommand { get; }
        public ICommand RunReportCommand { get; }
        public ICommand SelectAllColumnsCommand { get; }
        public ICommand ClearColumnsCommand { get; }
        public ICommand AddFilterCommand { get; }
        public ICommand RemoveFilterCommand { get; }
        public ICommand AddJoinCommand { get; }
        public ICommand RemoveJoinCommand { get; }
        public ICommand StartNewLinkCommand { get; }
        public ICommand OpenDesignerPopupCommand { get; }
        public ICommand CloseDesignerPopupCommand { get; }
        public ICommand EditLinkCommand { get; }
        public ICommand SaveLinkCommand { get; }
        public ICommand DeleteLinkCommand { get; }
        public ICommand ExportResultsCommand { get; }

        public ReportTableDto? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (ReferenceEquals(_selectedTable, value)) return;
                _selectedTable = value;
                OnPropertyChanged(nameof(SelectedTable));

                if (!_suppressSelectedTableLoad)
                {
                    LoadSelectedTableDesignAsync().SafeFireAndForget(HandleBackgroundException);
                }
            }
        }

        public ReportBuilderColumnOption? SelectedSortColumn
        {
            get => _selectedSortColumn;
            set
            {
                if (ReferenceEquals(_selectedSortColumn, value)) return;
                _selectedSortColumn = value;
                OnPropertyChanged(nameof(SelectedSortColumn));
            }
        }

        public ReportTableDto? SelectedDesignerSourceTable
        {
            get => _selectedDesignerSourceTable;
            set
            {
                if (ReferenceEquals(_selectedDesignerSourceTable, value)) return;
                _selectedDesignerSourceTable = value;
                OnPropertyChanged(nameof(SelectedDesignerSourceTable));
                OnPropertyChanged(nameof(DesignerRelationPreviewText));

                if (!_suppressDesignerSelectionChange)
                {
                    LoadDesignerSourceColumnsAsync().SafeFireAndForget(HandleBackgroundException);
                }
            }
        }

        public ReportTableDto? SelectedDesignerTargetTable
        {
            get => _selectedDesignerTargetTable;
            set
            {
                if (ReferenceEquals(_selectedDesignerTargetTable, value)) return;
                _selectedDesignerTargetTable = value;
                OnPropertyChanged(nameof(SelectedDesignerTargetTable));
                OnPropertyChanged(nameof(DesignerRelationPreviewText));

                if (!_suppressDesignerSelectionChange)
                {
                    LoadDesignerTargetColumnsAsync().SafeFireAndForget(HandleBackgroundException);
                }
            }
        }

        public ReportColumnDto? SelectedDesignerSourceColumn
        {
            get => _selectedDesignerSourceColumn;
            set
            {
                if (ReferenceEquals(_selectedDesignerSourceColumn, value)) return;
                _selectedDesignerSourceColumn = value;
                OnPropertyChanged(nameof(SelectedDesignerSourceColumn));
                OnPropertyChanged(nameof(DesignerRelationPreviewText));
            }
        }

        public ReportColumnDto? SelectedDesignerTargetColumn
        {
            get => _selectedDesignerTargetColumn;
            set
            {
                if (ReferenceEquals(_selectedDesignerTargetColumn, value)) return;
                _selectedDesignerTargetColumn = value;
                OnPropertyChanged(nameof(SelectedDesignerTargetColumn));
                OnPropertyChanged(nameof(DesignerRelationPreviewText));
            }
        }

        public ReportBuilderJoinTypeOption? SelectedJoinType
        {
            get => _selectedJoinType;
            set
            {
                if (ReferenceEquals(_selectedJoinType, value)) return;
                _selectedJoinType = value;
                OnPropertyChanged(nameof(SelectedJoinType));
                OnPropertyChanged(nameof(DesignerRelationPreviewText));
            }
        }

        public ReportTableLinkDto? SelectedCustomLink
        {
            get => _selectedCustomLink;
            set
            {
                if (ReferenceEquals(_selectedCustomLink, value)) return;
                _selectedCustomLink = value;
                OnPropertyChanged(nameof(SelectedCustomLink));
                OnPropertyChanged(nameof(HasSelectedCustomLink));

                if (!_suppressDesignerSelectionChange && value != null)
                {
                    LoadSelectedCustomLinkAsync(value).SafeFireAndForget(HandleBackgroundException);
                }
            }
        }

        public bool IsDesignerPopupOpen
        {
            get => _isDesignerPopupOpen;
            set
            {
                if (_isDesignerPopupOpen == value) return;
                _isDesignerPopupOpen = value;
                OnPropertyChanged(nameof(IsDesignerPopupOpen));
            }
        }

        public string DesignerLinkName
        {
            get => _designerLinkName;
            set
            {
                if (_designerLinkName == value) return;
                _designerLinkName = value;
                OnPropertyChanged(nameof(DesignerLinkName));
            }
        }

        public string DesignerRelationPreviewText
        {
            get
            {
                if (SelectedDesignerSourceTable == null || SelectedDesignerTargetTable == null)
                {
                    return "Choose a source table and a target table, then drag one column onto the other.";
                }

                if (SelectedDesignerSourceColumn == null || SelectedDesignerTargetColumn == null)
                {
                    return $"Drag a column from {SelectedDesignerSourceTable.DisplayName} to {SelectedDesignerTargetTable.DisplayName} to create the link.";
                }

                var joinText = SelectedJoinType?.DisplayName ?? "Join";
                return $"{SelectedDesignerSourceTable.DisplayName}.{SelectedDesignerSourceColumn.DisplayName} -> {SelectedDesignerTargetTable.DisplayName}.{SelectedDesignerTargetColumn.DisplayName} ({joinText})";
            }
        }

        public string DesignerStatus
        {
            get => _designerStatus;
            private set
            {
                if (_designerStatus == value) return;
                _designerStatus = value;
                OnPropertyChanged(nameof(DesignerStatus));
            }
        }

        public Brush DesignerStatusBrush
        {
            get => _designerStatusBrush;
            private set
            {
                if (_designerStatusBrush == value) return;
                _designerStatusBrush = value;
                OnPropertyChanged(nameof(DesignerStatusBrush));
            }
        }

        public int MaxRows
        {
            get => _maxRows;
            set
            {
                var normalized = Math.Clamp(value, 1, 5000);
                if (_maxRows == normalized) return;
                _maxRows = normalized;
                OnPropertyChanged(nameof(MaxRows));
            }
        }

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (_sortDescending == value) return;
                _sortDescending = value;
                OnPropertyChanged(nameof(SortDescending));
            }
        }

        public DataView? ResultView
        {
            get => _resultView;
            private set
            {
                if (ReferenceEquals(_resultView, value)) return;
                _resultView = value;
                OnPropertyChanged(nameof(ResultView));
                OnPropertyChanged(nameof(HasResult));
                OnPropertyChanged(nameof(ResultRowsText));
                OnPropertyChanged(nameof(ResultColumnsText));
                OnPropertyChanged(nameof(ResultViewerHint));
                OnPropertyChanged(nameof(CanExportCurrentResult));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value) return;
                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string ResultSummary
        {
            get => _resultSummary;
            private set
            {
                if (_resultSummary == value) return;
                _resultSummary = value;
                OnPropertyChanged(nameof(ResultSummary));
            }
        }

        public string ResultTableName
        {
            get => _resultTableName;
            private set
            {
                if (_resultTableName == value) return;
                _resultTableName = value;
                OnPropertyChanged(nameof(ResultTableName));
                OnPropertyChanged(nameof(ResultViewerTitle));
            }
        }

        public bool HasResult => ResultView != null;

        public string ResultViewerTitle => HasResult
            ? $"Report Viewer • {ResultTableName}"
            : "Report Viewer";

        public string ResultViewerHint => HasResult
            ? HasGroupedResult
                ? "Double-click a summary row to inspect it or drill into the source rows. You can also review the SQL preview before exporting."
                : "Preview the current result set, open row details, review the SQL preview, then export it to Excel, PDF, CSV, or JSON."
            : "Run a report on the left to preview it here and unlock exports.";

        public string ResultRowsText => ResultView == null
            ? "0 rows"
            : $"{ResultView.Count:N0} row(s)";

        public string ResultColumnsText => ResultView?.Table?.Columns.Count > 0
            ? $"{ResultView.Table.Columns.Count:N0} column(s)"
            : "0 columns";

        public string LastRunText => _lastResultRunAt.HasValue
            ? $"Last run on {_lastResultRunAt.Value:dd MMM yyyy HH:mm}"
            : "No report has been run yet.";

        public async Task LoadMetadataAsync()
        {
            IsLoading = true;
            SetStatus("Reading database tables and joins...", true);

            try
            {
                var selectedTableKey = SelectedTable?.Key;
                var selectedCustomLinkId = SelectedCustomLink?.Id;
                var tables = await _api.GetTablesAsync();
                var links = await _api.GetLinksAsync();

                _columnCache.Clear();
                Tables.Clear();
                foreach (var table in tables.OrderBy(t => t.SchemaName).ThenBy(t => t.TableName))
                {
                    Tables.Add(table);
                }

                ApplyLinks(links, selectedCustomLinkId);
                await LoadProMetadataAsync();

                _suppressSelectedTableLoad = true;
                SelectedTable = Tables.FirstOrDefault(table => string.Equals(table.Key, selectedTableKey, StringComparison.OrdinalIgnoreCase))
                    ?? Tables.FirstOrDefault();
                _suppressSelectedTableLoad = false;

                if (SelectedTable == null)
                {
                    ClearReportDesigner();
                    SetStatus("No reportable tables were found in the database.", false);
                    return;
                }

                await LoadSelectedTableDesignAsync(manageLoading: false);
                SetStatus($"Loaded {Tables.Count:N0} table(s) and {CustomLinks.Count:N0} custom link(s).", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Could not load report metadata: {ex.Message}", false);
            }
            finally
            {
                _suppressSelectedTableLoad = false;
                IsLoading = false;
            }
        }

        public async Task RunReportAsync()
        {
            if (SelectedTable == null)
            {
                SetStatus("Choose a table before running the report.", false);
                return;
            }

            var selectedColumns = Columns
                .Where(column => column.IsSelected)
                .Select(column => column.Key)
                .ToList();
            var hasCalculatedFields = CalculatedFields.Any(row => !string.IsNullOrWhiteSpace(row.Expression));
            var hasSummaryDesign = GroupByColumns.Any(row => row.SelectedColumn != null)
                                   || AggregateRows.Any(row => row.SelectedAggregateType != null
                                                               && (!row.NeedsSourceColumn || row.SelectedColumn != null));

            if (selectedColumns.Count == 0 && !hasCalculatedFields && !hasSummaryDesign)
            {
                SetStatus("Choose at least one column, calculated field, group, or total before running the report.", false);
                return;
            }

            IsLoading = true;
            SetStatus("Running report...", true);

            try
            {
                var request = new RunReportRequest
                {
                    TableKey = SelectedTable.Key,
                    Columns = selectedColumns,
                    Joins = SelectedJoins
                        .Select((link, index) => new ReportJoinRequest
                        {
                            LinkKey = link.LinkKey,
                            SortOrder = index
                        })
                        .ToList(),
                    Filters = Filters
                        .Where(filter => filter.SelectedColumn != null && filter.SelectedOperator != null)
                        .Select(filter => new ReportFilterRequest
                        {
                            ColumnKey = filter.SelectedColumn!.Key,
                            ColumnName = filter.SelectedColumn.ColumnName,
                            Operator = filter.SelectedOperator!.Key,
                            Value = filter.Value,
                            ValueTo = filter.ValueTo
                        })
                        .ToList(),
                    SortColumn = SelectedSortColumn?.Key,
                    SortDescending = SortDescending,
                    MaxRows = MaxRows
                };
                PopulateAdvancedRequest(request);

                var result = await _api.RunReportAsync(request);
                ResultView = BuildResultView(result);
                ResultTableName = string.IsNullOrWhiteSpace(result.TableName)
                    ? SelectedTable.DisplayName
                    : result.TableName;
                _lastResultRunAt = DateTime.Now;
                ResultSummary = $"{result.RowCount:N0} row(s), {result.Columns.Count:N0} column(s), limit {result.MaxRows:N0}.";
                ApplyAdvancedResultState(request, result);
                UpdateResultColumnsForCharts();
                RebuildCharts();
                SetStatus($"Report loaded from {result.TableName}.", true);
                OnPropertyChanged(nameof(LastRunText));
            }
            catch (Exception ex)
            {
                SetStatus($"Report failed: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSelectedTableDesignAsync(bool manageLoading = true)
        {
            if (SelectedTable == null)
            {
                ClearReportDesigner();
                return;
            }

            if (manageLoading)
            {
                IsLoading = true;
                SetStatus($"Reading columns for {SelectedTable.DisplayName}...", true);
            }

            try
            {
                await EnsureTableColumnsLoadedAsync(SelectedTable.Key);
                ResetAdvancedDesign();
                SelectedJoins.Clear();
                Filters.Clear();
                RefreshAvailableLinks();
                RebuildColumns(initializeDefaults: true);
                ClearCurrentResult(SelectedTable.DisplayName);
                await RefreshSchemaInspectorAsync();
                SetStatus($"Loaded {Columns.Count:N0} column(s) for {SelectedTable.DisplayName}.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Could not load report design: {ex.Message}", false);
            }
            finally
            {
                if (manageLoading)
                {
                    IsLoading = false;
                }
            }
        }

        private void ExportResults(string? formatKey)
        {
            if (ResultView == null)
            {
                SetStatus("Run a report before exporting.", false);
                return;
            }

            if (!CanExportCurrentResult)
            {
                SetStatus("This saved report is not allowed to export for your role.", false);
                return;
            }

            var format = NormalizeExportFormat(formatKey);
            var dialog = BuildExportDialog(format);
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            IsLoading = true;
            SetStatus($"Exporting report to {format.ToUpperInvariant()}...", true);

            try
            {
                var generatedAt = _lastResultRunAt ?? DateTime.Now;
                var filePath = dialog.FileName;
                var title = ResultTableName;
                var summary = ResultSummary;

                switch (format)
                {
                    case "xls":
                        ReportExportWriter.WriteExcelXml(ResultView, filePath, ResultTableName, title, summary, generatedAt);
                        break;
                    case "pdf":
                        ReportExportWriter.WritePdf(ResultView, filePath, title, summary, generatedAt);
                        break;
                    case "json":
                        ReportExportWriter.WriteJson(ResultView, filePath);
                        break;
                    default:
                        ReportExportWriter.WriteCsv(ResultView, filePath);
                        break;
                }

                SetStatus($"Report exported to {Path.GetFileName(filePath)}.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Could not export report: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddJoinAsync(ReportTableLinkDto link)
        {
            if (SelectedTable == null)
            {
                SetStatus("Choose a base table before adding joins.", false);
                return;
            }

            if (SelectedJoins.Any(existing => string.Equals(existing.LinkKey, link.LinkKey, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            SelectedJoins.Add(link);
            NormalizeSelectedJoins();
            foreach (var tableKey in GetActiveTableKeys())
            {
                await EnsureTableColumnsLoadedAsync(tableKey);
            }

            RebuildColumns();
            RefreshAvailableLinks();
            SetStatus($"Added join: {link.Description}.", true);
        }

        private async Task RemoveJoinAsync(ReportTableLinkDto link)
        {
            var toRemove = SelectedJoins.FirstOrDefault(existing => string.Equals(existing.LinkKey, link.LinkKey, StringComparison.OrdinalIgnoreCase));
            if (toRemove == null)
            {
                return;
            }

            SelectedJoins.Remove(toRemove);
            NormalizeSelectedJoins();
            foreach (var tableKey in GetActiveTableKeys())
            {
                await EnsureTableColumnsLoadedAsync(tableKey);
            }

            RebuildColumns();
            RefreshAvailableLinks();
            SetStatus($"Removed join: {link.Description}.", true);
        }

        private async Task SaveLinkAsync()
        {
            if (SelectedDesignerSourceTable == null || SelectedDesignerTargetTable == null)
            {
                SetDesignerStatus("Choose both source and target tables.", false);
                return;
            }

            if (SelectedDesignerSourceColumn == null || SelectedDesignerTargetColumn == null)
            {
                SetDesignerStatus("Choose both source and target columns.", false);
                return;
            }

            if (SelectedJoinType == null)
            {
                SetDesignerStatus("Choose a join type.", false);
                return;
            }

            try
            {
                var saved = await _api.SaveLinkAsync(new SaveReportTableLinkRequest
                {
                    Id = SelectedCustomLink?.Id,
                    LinkName = DesignerLinkName,
                    SourceTableKey = SelectedDesignerSourceTable.Key,
                    SourceColumnName = SelectedDesignerSourceColumn.ColumnName,
                    TargetTableKey = SelectedDesignerTargetTable.Key,
                    TargetColumnName = SelectedDesignerTargetColumn.ColumnName,
                    JoinType = SelectedJoinType.Key
                });

                await ReloadLinksAsync(saved.Id);
                IsDesignerPopupOpen = false;
                SetDesignerStatus($"Saved link '{saved.LinkName}'.", true);
                SetStatus($"Custom table link '{saved.LinkName}' is ready for reports.", true);
            }
            catch (Exception ex)
            {
                SetDesignerStatus($"Could not save link: {ex.Message}", false);
            }
        }

        private async Task DeleteSelectedLinkAsync()
        {
            if (SelectedCustomLink?.Id is not > 0)
            {
                SetDesignerStatus("Choose a custom link before deleting.", false);
                return;
            }

            try
            {
                var deletedName = SelectedCustomLink.LinkName;
                await _api.DeleteLinkAsync(SelectedCustomLink.Id.Value);
                await ReloadLinksAsync();
                ResetDesignerForm();
                IsDesignerPopupOpen = false;
                SetDesignerStatus($"Deleted link '{deletedName}'.", true);
                SetStatus($"Custom table link '{deletedName}' was deleted.", true);
            }
            catch (Exception ex)
            {
                SetDesignerStatus($"Could not delete link: {ex.Message}", false);
            }
        }

        private async Task EditLinkAsync(ReportTableLinkDto? link)
        {
            if (link == null)
            {
                SetDesignerStatus("Choose a saved link before editing.", false);
                return;
            }

            if (link.IsSystemDefined)
            {
                ResetDesignerForm();
                await LoadLinkIntoDesignerAsync(link);

                DesignerLinkName = string.IsNullOrWhiteSpace(link.LinkName)
                    ? BuildDefaultDesignerLinkName()
                    : $"{link.LinkName} Custom";

                IsDesignerPopupOpen = true;
                SetDesignerStatus("Built-in relation loaded as a custom draft. Save it to create your own editable link.", true);
                return;
            }

            _suppressDesignerSelectionChange = true;
            SelectedCustomLink = link;
            _suppressDesignerSelectionChange = false;

            await LoadLinkIntoDesignerAsync(link);
            IsDesignerPopupOpen = true;
            SetDesignerStatus($"Editing link '{link.LinkName}'.", true);
        }

        private async Task LoadSelectedCustomLinkAsync(ReportTableLinkDto link)
        {
            await LoadLinkIntoDesignerAsync(link);
            SetDesignerStatus($"Editing link '{link.LinkName}'.", true);
        }

        private async Task LoadLinkIntoDesignerAsync(ReportTableLinkDto link)
        {
            _suppressDesignerSelectionChange = true;

            SelectedDesignerSourceTable = Tables.FirstOrDefault(table => string.Equals(table.Key, link.SourceTableKey, StringComparison.OrdinalIgnoreCase));
            SelectedDesignerTargetTable = Tables.FirstOrDefault(table => string.Equals(table.Key, link.TargetTableKey, StringComparison.OrdinalIgnoreCase));
            DesignerLinkName = link.LinkName;
            SelectedJoinType = JoinTypeOptions.FirstOrDefault(option => string.Equals(option.Key, link.JoinType, StringComparison.OrdinalIgnoreCase))
                ?? JoinTypeOptions.FirstOrDefault();

            _suppressDesignerSelectionChange = false;

            await LoadDesignerSourceColumnsAsync(link.SourceColumnName);
            await LoadDesignerTargetColumnsAsync(link.TargetColumnName);
        }

        public void ApplyDesignerColumnDrop(string sourceRole, ReportColumnDto draggedColumn, string targetRole, ReportColumnDto droppedColumn)
        {
            if (draggedColumn == null || droppedColumn == null)
            {
                return;
            }

            if (string.Equals(sourceRole, targetRole, StringComparison.OrdinalIgnoreCase))
            {
                SetDesignerStatus("Drop the column on the opposite table card.", false);
                return;
            }

            if (string.Equals(sourceRole, "Source", StringComparison.OrdinalIgnoreCase))
            {
                SelectedDesignerSourceColumn = DesignerSourceColumns.FirstOrDefault(column => string.Equals(column.Key, draggedColumn.Key, StringComparison.OrdinalIgnoreCase)) ?? draggedColumn;
                SelectedDesignerTargetColumn = DesignerTargetColumns.FirstOrDefault(column => string.Equals(column.Key, droppedColumn.Key, StringComparison.OrdinalIgnoreCase)) ?? droppedColumn;
            }
            else
            {
                SelectedDesignerSourceColumn = DesignerSourceColumns.FirstOrDefault(column => string.Equals(column.Key, droppedColumn.Key, StringComparison.OrdinalIgnoreCase)) ?? droppedColumn;
                SelectedDesignerTargetColumn = DesignerTargetColumns.FirstOrDefault(column => string.Equals(column.Key, draggedColumn.Key, StringComparison.OrdinalIgnoreCase)) ?? draggedColumn;
            }

            if (string.IsNullOrWhiteSpace(DesignerLinkName))
            {
                DesignerLinkName = BuildDefaultDesignerLinkName();
            }

            SetDesignerStatus("Relation ready. Review the join type, then save the link.", true);
        }

        private async Task LoadDesignerSourceColumnsAsync(string? selectedColumnName = null)
        {
            DesignerSourceColumns.Clear();
            SelectedDesignerSourceColumn = null;

            if (SelectedDesignerSourceTable == null)
            {
                return;
            }

            var columns = await EnsureTableColumnsLoadedAsync(SelectedDesignerSourceTable.Key);
            foreach (var column in columns.OrderBy(column => column.OrdinalPosition))
            {
                DesignerSourceColumns.Add(column);
            }

            SelectedDesignerSourceColumn = DesignerSourceColumns.FirstOrDefault(column =>
                                               string.Equals(column.ColumnName, selectedColumnName, StringComparison.OrdinalIgnoreCase))
                                           ?? DesignerSourceColumns.FirstOrDefault();
        }

        private async Task LoadDesignerTargetColumnsAsync(string? selectedColumnName = null)
        {
            DesignerTargetColumns.Clear();
            SelectedDesignerTargetColumn = null;

            if (SelectedDesignerTargetTable == null)
            {
                return;
            }

            var columns = await EnsureTableColumnsLoadedAsync(SelectedDesignerTargetTable.Key);
            foreach (var column in columns.OrderBy(column => column.OrdinalPosition))
            {
                DesignerTargetColumns.Add(column);
            }

            SelectedDesignerTargetColumn = DesignerTargetColumns.FirstOrDefault(column =>
                                               string.Equals(column.ColumnName, selectedColumnName, StringComparison.OrdinalIgnoreCase))
                                           ?? DesignerTargetColumns.FirstOrDefault();
        }

        private async Task<List<ReportColumnDto>> EnsureTableColumnsLoadedAsync(string tableKey)
        {
            if (_columnCache.TryGetValue(tableKey, out var cached))
            {
                return cached;
            }

            var columns = await _api.GetColumnsAsync(tableKey);
            _columnCache[tableKey] = columns
                .OrderBy(column => column.OrdinalPosition)
                .ToList();

            return _columnCache[tableKey];
        }

        private void ApplyLinks(IEnumerable<ReportTableLinkDto> links, int? selectedCustomLinkId = null)
        {
            var allLinks = links
                .OrderBy(link => link.SourceTableDisplayName)
                .ThenBy(link => link.TargetTableDisplayName)
                .ThenBy(link => link.LinkName)
                .ToList();
            _allLinks.Clear();
            _allLinks.AddRange(allLinks);

            var selectedJoinKeys = SelectedJoins.Select(link => link.LinkKey).ToList();

            CustomLinks.Clear();
            foreach (var link in allLinks.Where(link => !link.IsSystemDefined))
            {
                CustomLinks.Add(link);
            }

            SelectedJoins.Clear();
            foreach (var joinKey in selectedJoinKeys)
            {
                var resolved = allLinks.FirstOrDefault(link => string.Equals(link.LinkKey, joinKey, StringComparison.OrdinalIgnoreCase));
                if (resolved != null)
                {
                    SelectedJoins.Add(resolved);
                }
            }

            NormalizeSelectedJoins();
            RefreshAvailableLinks(allLinks);

            _suppressDesignerSelectionChange = true;
            SelectedCustomLink = selectedCustomLinkId.HasValue
                ? CustomLinks.FirstOrDefault(link => link.Id == selectedCustomLinkId.Value)
                : null;
            _suppressDesignerSelectionChange = false;

            if (SelectedCustomLink != null)
            {
                LoadSelectedCustomLinkAsync(SelectedCustomLink).SafeFireAndForget(HandleBackgroundException);
            }

            OnPropertyChanged(nameof(CustomLinkCountText));
            OnPropertyChanged(nameof(AvailableLinkCountText));
            OnPropertyChanged(nameof(SelectedJoinCountText));
        }

        private async Task ReloadLinksAsync(int? selectedCustomLinkId = null)
        {
            var links = await _api.GetLinksAsync();
            ApplyLinks(links, selectedCustomLinkId);
            RefreshAvailableLinks();
            RebuildColumns();
        }

        private void RefreshAvailableLinks(IEnumerable<ReportTableLinkDto>? source = null)
        {
            AvailableLinks.Clear();

            if (SelectedTable == null)
            {
                OnPropertyChanged(nameof(AvailableLinkCountText));
                return;
            }

            var activeTables = GetActiveTableKeys().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selectedJoinKeys = SelectedJoins.Select(link => link.LinkKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var link in (source ?? _allLinks)
                         .Where(link => !selectedJoinKeys.Contains(link.LinkKey))
                         .Where(link =>
                         {
                             var sourceActive = activeTables.Contains(link.SourceTableKey);
                             var targetActive = activeTables.Contains(link.TargetTableKey);
                             return sourceActive ^ targetActive;
                         })
                         .OrderBy(link => link.TargetTableDisplayName)
                         .ThenBy(link => link.LinkName))
            {
                AvailableLinks.Add(link);
            }

            OnPropertyChanged(nameof(AvailableLinkCountText));
            OnPropertyChanged(nameof(SelectedJoinCountText));
        }

        private void NormalizeSelectedJoins()
        {
            if (SelectedTable == null)
            {
                SelectedJoins.Clear();
                return;
            }

            var activeTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SelectedTable.Key };
            var normalized = new List<ReportTableLinkDto>();

            foreach (var link in SelectedJoins.ToList())
            {
                var sourceActive = activeTables.Contains(link.SourceTableKey);
                var targetActive = activeTables.Contains(link.TargetTableKey);

                if (sourceActive == targetActive)
                {
                    continue;
                }

                var newTableKey = sourceActive ? link.TargetTableKey : link.SourceTableKey;
                if (!activeTables.Add(newTableKey))
                {
                    continue;
                }

                normalized.Add(link);
            }

            if (normalized.Count == SelectedJoins.Count
                && normalized.Zip(SelectedJoins, (left, right) => string.Equals(left.LinkKey, right.LinkKey, StringComparison.OrdinalIgnoreCase)).All(value => value))
            {
                return;
            }

            SelectedJoins.Clear();
            foreach (var link in normalized)
            {
                SelectedJoins.Add(link);
            }

            OnPropertyChanged(nameof(SelectedJoinCountText));
        }

        private IReadOnlyList<string> GetActiveTableKeys()
        {
            if (SelectedTable == null)
            {
                return Array.Empty<string>();
            }

            var activeTableKeys = new List<string> { SelectedTable.Key };
            var activeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SelectedTable.Key };

            foreach (var link in SelectedJoins)
            {
                if (activeSet.Contains(link.SourceTableKey) && activeSet.Add(link.TargetTableKey))
                {
                    activeTableKeys.Add(link.TargetTableKey);
                }
                else if (activeSet.Contains(link.TargetTableKey) && activeSet.Add(link.SourceTableKey))
                {
                    activeTableKeys.Add(link.SourceTableKey);
                }
            }

            return activeTableKeys;
        }

        private void RebuildColumns(bool initializeDefaults = false)
        {
            var selectedColumnKeys = Columns.Where(column => column.IsSelected)
                .Select(column => column.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selectedSortKey = SelectedSortColumn?.Key;
            var filterColumnKeys = Filters
                .Select(filter => filter.SelectedColumn?.Key)
                .ToList();

            Columns.Clear();

            var activeTableKeys = GetActiveTableKeys();
            foreach (var tableKey in activeTableKeys)
            {
                if (!_columnCache.TryGetValue(tableKey, out var columnDtos))
                {
                    continue;
                }

                foreach (var column in columnDtos.OrderBy(item => item.OrdinalPosition))
                {
                    var option = new ReportBuilderColumnOption(column)
                    {
                        IsSelected = initializeDefaults
                            ? string.Equals(tableKey, SelectedTable?.Key, StringComparison.OrdinalIgnoreCase)
                                && Columns.Count(existing => existing.IsSelected) < 8
                            : selectedColumnKeys.Contains(column.Key)
                    };

                    option.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(ReportBuilderColumnOption.IsSelected))
                        {
                            OnPropertyChanged(nameof(SelectedColumnCountText));
                        }
                    };

                    Columns.Add(option);
                }
            }

            for (var index = 0; index < Filters.Count; index++)
            {
                var key = filterColumnKeys.ElementAtOrDefault(index);
                Filters[index].SelectedColumn = Columns.FirstOrDefault(column => string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase))
                    ?? Columns.FirstOrDefault();
            }

            SelectedSortColumn = Columns.FirstOrDefault(column => string.Equals(column.Key, selectedSortKey, StringComparison.OrdinalIgnoreCase))
                ?? Columns.FirstOrDefault();

            RebindAdvancedColumnSelections();
            RebindCurrencyColumnSelections();
            OnPropertyChanged(nameof(SelectedColumnCountText));
        }

        private void AddFilter()
        {
            if (Columns.Count == 0)
            {
                SetStatus("Choose a table before adding filters.", false);
                return;
            }

            Filters.Add(new ReportBuilderFilterRow
            {
                SelectedColumn = Columns.FirstOrDefault(),
                SelectedOperator = OperatorOptions.FirstOrDefault(option => option.Key == "contains")
                    ?? OperatorOptions.FirstOrDefault()
            });
        }

        private void SetAllColumns(bool isSelected)
        {
            foreach (var column in Columns)
            {
                column.IsSelected = isSelected;
            }

            OnPropertyChanged(nameof(SelectedColumnCountText));
        }

        private void ClearReportDesigner()
        {
            ResetAdvancedDesign();
            Columns.Clear();
            Filters.Clear();
            SelectedJoins.Clear();
            AvailableLinks.Clear();
            ClearCurrentResult();
            SelectedSortColumn = null;
            OnPropertyChanged(nameof(AvailableLinkCountText));
            OnPropertyChanged(nameof(SelectedJoinCountText));
            OnPropertyChanged(nameof(SelectedColumnCountText));
        }

        private void ClearCurrentResult(string? tableName = null)
        {
            ResultView = null;
            ResultSummary = "No report has been run yet.";
            ResultTableName = string.IsNullOrWhiteSpace(tableName) ? "No report selected." : tableName;
            _lastResultRunAt = null;
            ClearAdvancedResultState();
            UpdateResultColumnsForCharts();
            RebuildCharts();
            OnPropertyChanged(nameof(LastRunText));
        }

        private static string NormalizeExportFormat(string? formatKey)
        {
            return formatKey?.Trim().ToLowerInvariant() switch
            {
                "pdf" => "pdf",
                "json" => "json",
                "csv" => "csv",
                _ => "xls"
            };
        }

        private SaveFileDialog BuildExportDialog(string format)
        {
            return new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                OverwritePrompt = true,
                DefaultExt = $".{format}",
                Filter = format switch
                {
                    "pdf" => "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
                    "json" => "JSON (*.json)|*.json|All files (*.*)|*.*",
                    "csv" => "CSV (*.csv)|*.csv|All files (*.*)|*.*",
                    _ => "Excel 2003 XML (*.xls)|*.xls|All files (*.*)|*.*"
                },
                FileName = BuildDefaultExportFileName(format),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
        }

        private string BuildDefaultExportFileName(string format)
        {
            var safeTableName = string.Concat(ResultTableName
                .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character))
                .Trim();

            if (string.IsNullOrWhiteSpace(safeTableName))
            {
                safeTableName = "Report";
            }

            return $"{safeTableName}-{DateTime.Now:yyyyMMdd-HHmmss}.{format}";
        }

        private void ResetDesignerForm()
        {
            _suppressDesignerSelectionChange = true;
            _selectedCustomLink = null;
            OnPropertyChanged(nameof(SelectedCustomLink));
            OnPropertyChanged(nameof(HasSelectedCustomLink));
            _selectedDesignerSourceTable = null;
            OnPropertyChanged(nameof(SelectedDesignerSourceTable));
            _selectedDesignerTargetTable = null;
            OnPropertyChanged(nameof(SelectedDesignerTargetTable));
            DesignerSourceColumns.Clear();
            DesignerTargetColumns.Clear();
            _selectedDesignerSourceColumn = null;
            OnPropertyChanged(nameof(SelectedDesignerSourceColumn));
            _selectedDesignerTargetColumn = null;
            OnPropertyChanged(nameof(SelectedDesignerTargetColumn));
            _suppressDesignerSelectionChange = false;

            DesignerLinkName = string.Empty;
            SelectedJoinType = JoinTypeOptions.FirstOrDefault();
            OnPropertyChanged(nameof(DesignerRelationPreviewText));
            SetDesignerStatus("Create a custom table link or use database foreign keys.", true);
        }

        private void BeginNewLink()
        {
            ResetDesignerForm();
            IsDesignerPopupOpen = true;
        }

        private void OpenDesignerPopup()
        {
            IsDesignerPopupOpen = true;
        }

        private string BuildDefaultDesignerLinkName()
        {
            if (SelectedDesignerSourceTable == null
                || SelectedDesignerTargetTable == null
                || SelectedDesignerSourceColumn == null
                || SelectedDesignerTargetColumn == null)
            {
                return string.Empty;
            }

            return $"{SelectedDesignerSourceTable.DisplayName} {SelectedDesignerSourceColumn.DisplayName} to {SelectedDesignerTargetTable.DisplayName} {SelectedDesignerTargetColumn.DisplayName}";
        }

        public string AvailableLinkCountText => $"{AvailableLinks.Count:N0} available join(s)";
        public string SelectedJoinCountText => $"{SelectedJoins.Count:N0} selected join(s)";
        public string SelectedColumnCountText => $"{Columns.Count(column => column.IsSelected):N0} of {Columns.Count:N0} column(s) selected";
        public string CustomLinkCountText => $"{CustomLinks.Count:N0} custom link(s)";
        public bool HasSelectedCustomLink => SelectedCustomLink != null;

        private static DataView BuildResultView(ReportResultDto result)
        {
            var table = new DataTable(result.TableName);
            var displayNames = BuildUniqueDisplayNames(result.Columns.Select(column => column.DisplayName));
            var columnNames = new List<(string ColumnName, string DisplayName)>();

            for (var index = 0; index < result.Columns.Count; index++)
            {
                table.Columns.Add(displayNames[index], typeof(object));
                columnNames.Add((result.Columns[index].ColumnName, displayNames[index]));
            }

            foreach (var sourceRow in result.Rows)
            {
                var row = table.NewRow();
                foreach (var column in columnNames)
                {
                    row[column.DisplayName] = sourceRow.TryGetValue(column.ColumnName, out var value)
                        ? NormalizeCellValue(value) ?? DBNull.Value
                        : DBNull.Value;
                }

                table.Rows.Add(row);
            }

            return table.DefaultView;
        }

        private static string BuildUniqueColumnName(DataTable table, string displayName)
        {
            var baseName = string.IsNullOrWhiteSpace(displayName) ? "Column" : displayName.Trim();
            var candidate = baseName;
            var index = 2;
            while (table.Columns.Contains(candidate))
            {
                candidate = $"{baseName} {index++}";
            }

            return candidate;
        }

        private static object? NormalizeCellValue(object? value)
        {
            if (value is not JsonElement element)
            {
                return value;
            }

            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
                JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
                _ => element.ToString()
            };
        }

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed;
        }

        private void SetDesignerStatus(string message, bool isSuccess)
        {
            DesignerStatus = message;
            DesignerStatusBrush = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed;
        }

        private void HandleBackgroundException(Exception ex)
        {
            SetStatus($"Report builder failed: {ex.Message}", false);
            IsLoading = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
