using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Reports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed partial class ReportBuilderViewModel
    {
        private readonly Dictionary<string, string> _displayNameByResultKey = new(StringComparer.OrdinalIgnoreCase);
        private RunReportRequest? _lastRunRequest;
        private ReportResultDto? _lastResultDto;
        private string _sqlPreviewText = string.Empty;
        private bool _isSqlPreviewVisible;
        private DataRowView? _selectedResultRow;
        private bool _isRowDetailsOpen;
        private string _rowDetailsTitle = "Row Details";

        public ObservableCollection<ReportBuilderCalculatedFieldRow> CalculatedFields { get; } = new();
        public ObservableCollection<ReportBuilderGroupByRow> GroupByColumns { get; } = new();
        public ObservableCollection<ReportBuilderAggregateTypeOption> AggregateTypeOptions { get; } = new();
        public ObservableCollection<ReportBuilderAggregateRow> AggregateRows { get; } = new();
        public ObservableCollection<ReportBuilderRowDetailItem> RowDetails { get; } = new();

        public ICommand AddCalculatedFieldCommand { get; private set; } = null!;
        public ICommand RemoveCalculatedFieldCommand { get; private set; } = null!;
        public ICommand AddGroupByCommand { get; private set; } = null!;
        public ICommand RemoveGroupByCommand { get; private set; } = null!;
        public ICommand AddAggregateCommand { get; private set; } = null!;
        public ICommand RemoveAggregateCommand { get; private set; } = null!;
        public ICommand ToggleSqlPreviewCommand { get; private set; } = null!;
        public ICommand CopySqlPreviewCommand { get; private set; } = null!;
        public ICommand OpenRowDetailsCommand { get; private set; } = null!;
        public ICommand CloseRowDetailsCommand { get; private set; } = null!;
        public ICommand DrillDownCommand { get; private set; } = null!;

        public string SqlPreviewText
        {
            get => _sqlPreviewText;
            private set
            {
                if (_sqlPreviewText == value) return;
                _sqlPreviewText = value;
                OnPropertyChanged(nameof(SqlPreviewText));
                OnPropertyChanged(nameof(HasSqlPreview));
            }
        }

        public bool HasSqlPreview => !string.IsNullOrWhiteSpace(SqlPreviewText);

        public bool IsSqlPreviewVisible
        {
            get => _isSqlPreviewVisible;
            set
            {
                if (_isSqlPreviewVisible == value) return;
                _isSqlPreviewVisible = value;
                OnPropertyChanged(nameof(IsSqlPreviewVisible));
            }
        }

        public DataRowView? SelectedResultRow
        {
            get => _selectedResultRow;
            set
            {
                if (ReferenceEquals(_selectedResultRow, value)) return;
                _selectedResultRow = value;
                OnPropertyChanged(nameof(SelectedResultRow));
                OnPropertyChanged(nameof(CanDrillDown));
            }
        }

        public bool IsRowDetailsOpen
        {
            get => _isRowDetailsOpen;
            set
            {
                if (_isRowDetailsOpen == value) return;
                _isRowDetailsOpen = value;
                OnPropertyChanged(nameof(IsRowDetailsOpen));
            }
        }

        public string RowDetailsTitle
        {
            get => _rowDetailsTitle;
            private set
            {
                if (_rowDetailsTitle == value) return;
                _rowDetailsTitle = value;
                OnPropertyChanged(nameof(RowDetailsTitle));
            }
        }

        public bool HasGroupedResult => _lastResultDto?.IsGrouped == true;

        public bool CanDrillDown
            => HasGroupedResult
               && SelectedResultRow != null
               && _lastRunRequest?.GroupByColumns?.Count > 0;

        private void InitializeAdvancedFeatures()
        {
            AggregateTypeOptions.Add(new ReportBuilderAggregateTypeOption { Key = "COUNT_ROWS", DisplayName = "Count rows" });
            AggregateTypeOptions.Add(new ReportBuilderAggregateTypeOption { Key = "COUNT", DisplayName = "Count values" });
            AggregateTypeOptions.Add(new ReportBuilderAggregateTypeOption { Key = "SUM", DisplayName = "Sum" });
            AggregateTypeOptions.Add(new ReportBuilderAggregateTypeOption { Key = "AVG", DisplayName = "Average" });
            AggregateTypeOptions.Add(new ReportBuilderAggregateTypeOption { Key = "MIN", DisplayName = "Minimum" });
            AggregateTypeOptions.Add(new ReportBuilderAggregateTypeOption { Key = "MAX", DisplayName = "Maximum" });
        }

        private void InitializeAdvancedCommands()
        {
            AddCalculatedFieldCommand = new RelayCommand(_ => AddCalculatedField());
            RemoveCalculatedFieldCommand = new RelayCommand(parameter =>
            {
                if (parameter is ReportBuilderCalculatedFieldRow row)
                {
                    CalculatedFields.Remove(row);
                }
            });
            AddGroupByCommand = new RelayCommand(_ => AddGroupBy());
            RemoveGroupByCommand = new RelayCommand(parameter =>
            {
                if (parameter is ReportBuilderGroupByRow row)
                {
                    GroupByColumns.Remove(row);
                }
            });
            AddAggregateCommand = new RelayCommand(_ => AddAggregate());
            RemoveAggregateCommand = new RelayCommand(parameter =>
            {
                if (parameter is ReportBuilderAggregateRow row)
                {
                    AggregateRows.Remove(row);
                }
            });
            ToggleSqlPreviewCommand = new RelayCommand(_ => IsSqlPreviewVisible = !IsSqlPreviewVisible);
            CopySqlPreviewCommand = new RelayCommand(_ => CopySqlPreview());
            OpenRowDetailsCommand = new RelayCommand(_ => OpenSelectedRowDetails());
            CloseRowDetailsCommand = new RelayCommand(_ => IsRowDetailsOpen = false);
            DrillDownCommand = new RelayCommand(_ => DrillDownAsync().SafeFireAndForget(HandleBackgroundException));
        }

        private void PopulateAdvancedRequest(RunReportRequest request)
        {
            request.CalculatedColumns = CalculatedFields
                .Where(row => !string.IsNullOrWhiteSpace(row.Expression))
                .Select(row => new ReportCalculatedColumnRequest
                {
                    Alias = row.Alias?.Trim() ?? string.Empty,
                    Expression = row.Expression?.Trim() ?? string.Empty
                })
                .ToList();

            request.GroupByColumns = GroupByColumns
                .Where(row => row.SelectedColumn != null)
                .Select(row => row.SelectedColumn!.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            request.Aggregates = AggregateRows
                .Where(row => row.SelectedAggregateType != null)
                .Where(row => !row.NeedsSourceColumn || row.SelectedColumn != null)
                .Select(row => new ReportAggregateRequest
                {
                    AggregateType = row.SelectedAggregateType!.Key,
                    Alias = row.Alias?.Trim() ?? string.Empty,
                    SourceIdentifier = row.NeedsSourceColumn ? row.SelectedColumn?.Key : null
                })
                .ToList();

            request.IncludeSqlPreview = true;

            if ((request.GroupByColumns.Count > 0 || request.Aggregates.Count > 0)
                && !string.IsNullOrWhiteSpace(request.SortColumn)
                && !request.GroupByColumns.Contains(request.SortColumn, StringComparer.OrdinalIgnoreCase))
            {
                request.SortColumn = null;
            }
        }

        private void ApplyAdvancedResultState(RunReportRequest request, ReportResultDto result)
        {
            _lastRunRequest = CloneRunRequest(request);
            _lastResultDto = result;
            SqlPreviewText = result.GeneratedSql ?? string.Empty;
            _displayNameByResultKey.Clear();

            var displayNames = BuildUniqueDisplayNames(result.Columns.Select(column => column.DisplayName));
            for (var index = 0; index < result.Columns.Count && index < displayNames.Count; index++)
            {
                _displayNameByResultKey[result.Columns[index].Key] = displayNames[index];
            }

            SelectedResultRow = null;
            RowDetails.Clear();
            RowDetailsTitle = result.IsGrouped ? "Summary Row Details" : "Row Details";
            IsRowDetailsOpen = false;
            OnPropertyChanged(nameof(HasGroupedResult));
            OnPropertyChanged(nameof(CanDrillDown));
        }

        private void ResetAdvancedDesign()
        {
            CalculatedFields.Clear();
            GroupByColumns.Clear();
            AggregateRows.Clear();
        }

        private void ClearAdvancedResultState()
        {
            _lastRunRequest = null;
            _lastResultDto = null;
            _displayNameByResultKey.Clear();
            SqlPreviewText = string.Empty;
            IsSqlPreviewVisible = false;
            SelectedResultRow = null;
            RowDetails.Clear();
            IsRowDetailsOpen = false;
            RowDetailsTitle = "Row Details";
            OnPropertyChanged(nameof(HasGroupedResult));
            OnPropertyChanged(nameof(CanDrillDown));
        }

        private void RebindAdvancedColumnSelections()
        {
            foreach (var groupRow in GroupByColumns)
            {
                var selectedKey = groupRow.SelectedColumn?.Key;
                groupRow.SelectedColumn = string.IsNullOrWhiteSpace(selectedKey)
                    ? Columns.FirstOrDefault()
                    : Columns.FirstOrDefault(column => string.Equals(column.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
                        ?? Columns.FirstOrDefault();
            }

            foreach (var aggregateRow in AggregateRows)
            {
                if (!aggregateRow.NeedsSourceColumn)
                {
                    aggregateRow.SelectedColumn = null;
                    continue;
                }

                var selectedKey = aggregateRow.SelectedColumn?.Key;
                aggregateRow.SelectedColumn = string.IsNullOrWhiteSpace(selectedKey)
                    ? Columns.FirstOrDefault()
                    : Columns.FirstOrDefault(column => string.Equals(column.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
                        ?? Columns.FirstOrDefault();
            }
        }

        private void AddCalculatedField()
        {
            CalculatedFields.Add(new ReportBuilderCalculatedFieldRow
            {
                Alias = $"Calculated {CalculatedFields.Count + 1}"
            });
        }

        private void AddGroupBy()
        {
            GroupByColumns.Add(new ReportBuilderGroupByRow
            {
                SelectedColumn = Columns.FirstOrDefault()
            });
        }

        private void AddAggregate()
        {
            AggregateRows.Add(new ReportBuilderAggregateRow
            {
                SelectedAggregateType = AggregateTypeOptions.FirstOrDefault(),
                SelectedColumn = Columns.FirstOrDefault(),
                Alias = string.Empty
            });
        }

        private void CopySqlPreview()
        {
            if (!HasSqlPreview)
            {
                SetStatus("Run a report before copying the SQL preview.", false);
                return;
            }

            Clipboard.SetText(SqlPreviewText);
            SetStatus("SQL preview copied to the clipboard.", true);
        }

        private void OpenSelectedRowDetails()
        {
            if (SelectedResultRow == null)
            {
                SetStatus("Choose a result row before opening details.", false);
                return;
            }

            RowDetails.Clear();
            foreach (DataColumn column in SelectedResultRow.Row.Table.Columns)
            {
                RowDetails.Add(new ReportBuilderRowDetailItem
                {
                    Label = column.ColumnName,
                    Value = FormatResultValue(SelectedResultRow.Row[column])
                });
            }

            RowDetailsTitle = HasGroupedResult ? "Summary Row Details" : "Row Details";
            IsRowDetailsOpen = true;
        }

        private async Task DrillDownAsync()
        {
            if (!CanDrillDown || _lastRunRequest == null || _lastResultDto == null || SelectedResultRow == null)
            {
                SetStatus("Choose a grouped result row before drilling down.", false);
                return;
            }

            var request = CloneRunRequest(_lastRunRequest);
            request.GroupByColumns = new List<string>();
            request.Aggregates = new List<ReportAggregateRequest>();
            request.CalculatedColumns = new List<ReportCalculatedColumnRequest>();
            request.SortColumn = null;
            request.SortDescending = false;
            request.Filters = request.Filters
                .Select(filter => new ReportFilterRequest
                {
                    ColumnKey = filter.ColumnKey,
                    ColumnName = filter.ColumnName,
                    Operator = filter.Operator,
                    Value = filter.Value,
                    ValueTo = filter.ValueTo
                })
                .ToList();

            foreach (var column in _lastResultDto.Columns.Where(column => string.Equals(column.SourceType, "group", StringComparison.OrdinalIgnoreCase)))
            {
                if (!_displayNameByResultKey.TryGetValue(column.Key, out var displayName)
                    || !SelectedResultRow.Row.Table.Columns.Contains(displayName))
                {
                    continue;
                }

                var cellValue = SelectedResultRow.Row[displayName];
                if (cellValue == null || cellValue == DBNull.Value)
                {
                    request.Filters.Add(new ReportFilterRequest
                    {
                        ColumnKey = column.Key,
                        Operator = "is_null"
                    });
                    continue;
                }

                request.Filters.Add(new ReportFilterRequest
                {
                    ColumnKey = column.Key,
                    Operator = "equals",
                    Value = ConvertFilterValue(cellValue)
                });
            }

            IsLoading = true;
            SetStatus("Running drill-down report...", true);

            try
            {
                var result = await _api.RunReportAsync(request);
                ResultView = BuildResultView(result);
                ResultTableName = string.IsNullOrWhiteSpace(result.TableName)
                    ? SelectedTable?.DisplayName ?? "Report"
                    : $"{result.TableName} Drill Down";
                _lastResultRunAt = DateTime.Now;
                ResultSummary = $"{result.RowCount:N0} row(s), {result.Columns.Count:N0} column(s), limit {result.MaxRows:N0}.";
                ApplyAdvancedResultState(request, result);
                UpdateResultColumnsForCharts();
                RebuildCharts();
                SetStatus("Drill-down report loaded.", true);
                OnPropertyChanged(nameof(LastRunText));
            }
            catch (Exception ex)
            {
                SetStatus($"Drill-down failed: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static RunReportRequest CloneRunRequest(RunReportRequest source)
        {
            return new RunReportRequest
            {
                TableKey = source.TableKey,
                Columns = source.Columns.ToList(),
                GroupByColumns = source.GroupByColumns.ToList(),
                CalculatedColumns = source.CalculatedColumns
                    .Select(column => new ReportCalculatedColumnRequest
                    {
                        Alias = column.Alias,
                        Expression = column.Expression
                    })
                    .ToList(),
                Aggregates = source.Aggregates
                    .Select(aggregate => new ReportAggregateRequest
                    {
                        AggregateType = aggregate.AggregateType,
                        Alias = aggregate.Alias,
                        SourceIdentifier = aggregate.SourceIdentifier
                    })
                    .ToList(),
                Joins = source.Joins
                    .Select(join => new ReportJoinRequest
                    {
                        LinkKey = join.LinkKey,
                        SortOrder = join.SortOrder
                    })
                    .ToList(),
                Filters = source.Filters
                    .Select(filter => new ReportFilterRequest
                    {
                        ColumnKey = filter.ColumnKey,
                        ColumnName = filter.ColumnName,
                        Operator = filter.Operator,
                        Value = filter.Value,
                        ValueTo = filter.ValueTo
                    })
                    .ToList(),
                SortColumn = source.SortColumn,
                SortDescending = source.SortDescending,
                MaxRows = source.MaxRows,
                IncludeSqlPreview = source.IncludeSqlPreview
            };
        }

        private static string ConvertFilterValue(object value)
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
                bool boolean => boolean ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string FormatResultValue(object? value)
        {
            return value switch
            {
                null or DBNull => string.Empty,
                DateTime dateTime => dateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture),
                bool boolean => boolean ? "Yes" : "No",
                _ => value.ToString() ?? string.Empty
            };
        }

        private static List<string> BuildUniqueDisplayNames(IEnumerable<string> displayNames)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();

            foreach (var name in displayNames)
            {
                var baseName = string.IsNullOrWhiteSpace(name) ? "Column" : name.Trim();
                var candidate = baseName;
                var index = 2;
                while (!used.Add(candidate))
                {
                    candidate = $"{baseName} {index++}";
                }

                result.Add(candidate);
            }

            return result;
        }
    }
}
