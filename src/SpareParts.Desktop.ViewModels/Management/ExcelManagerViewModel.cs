using Microsoft.Win32;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelManagerViewModel : INotifyPropertyChanged
    {
        private const string NotMappedHeader = "(Not mapped)";

        private readonly ManagementCoordinator _coordinator;
        private readonly Func<ExcelImportExecutionContext> _contextFactory;
        private readonly Func<Task> _refreshAllAsync;
        private readonly Action<string, bool> _statusReporter;
        private IReadOnlyCollection<AppConstantDto> _appConstants = Array.Empty<AppConstantDto>();
        private readonly Dictionary<string, IReadOnlyDictionary<string, string?>> _localMappings = new(StringComparer.OrdinalIgnoreCase);
        private ExcelImportTargetOption? _selectedTarget;
        private string _workbookPath = string.Empty;
        private string _workbookSummary = "Choose a table and workbook to start mapping.";
        private string _status = "Excel manager is ready.";
        private bool _isBusy;

        public ObservableCollection<ExcelImportTargetOption> Targets { get; } = new();
        public ObservableCollection<ExcelImportFieldMappingRow> MappingRows { get; } = new();
        public ObservableCollection<string> WorkbookHeaders { get; } = new();
        public ObservableCollection<string> WorkbookHeaderOptions { get; } = new();

        public ExcelImportTargetOption? SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                if (_selectedTarget == value)
                {
                    return;
                }

                _selectedTarget = value;
                OnPropertyChanged(nameof(SelectedTarget));
                OnPropertyChanged(nameof(SelectedTargetSummary));
                OnPropertyChanged(nameof(CanSaveMapping));
                OnPropertyChanged(nameof(CanImport));
                RebuildMappingRows();
            }
        }

        public string WorkbookPath
        {
            get => _workbookPath;
             set
            {
                if (_workbookPath == value)
                {
                    return;
                }

                _workbookPath = value;
                OnPropertyChanged(nameof(WorkbookPath));
                OnPropertyChanged(nameof(CanImport));
            }
        }


        public string WorkbookSummary
        {
            get => _workbookSummary;
            private set
            {
                if (_workbookSummary == value)
                {
                    return;
                }

                _workbookSummary = value;
                OnPropertyChanged(nameof(WorkbookSummary));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanSaveMapping));
                OnPropertyChanged(nameof(CanImport));
            }
        }

        public bool CanSaveMapping => !IsBusy && SelectedTarget != null && MappingRows.Count > 0;
        public bool CanImport => CanSaveMapping && !string.IsNullOrWhiteSpace(WorkbookPath);

        public string SelectedTargetSummary =>
            SelectedTarget == null
                ? "Choose any system table to configure how Excel headers should map into it."
                : $"{SelectedTarget.TableName} is ready with {SelectedTarget.ColumnCount} import column(s).";

        public ICommand BrowseWorkbookCommand { get; }
        public ICommand AutoMatchCommand { get; }
        public ICommand SaveMappingCommand { get; }
        public ICommand ImportCommand { get; }

        public ExcelManagerViewModel(
            ManagementCoordinator coordinator,
            Func<ExcelImportExecutionContext> contextFactory,
            Func<Task> refreshAllAsync,
            Action<string, bool> statusReporter)
        {
            _coordinator = coordinator;
            _contextFactory = contextFactory;
            _refreshAllAsync = refreshAllAsync;
            _statusReporter = statusReporter;

            BrowseWorkbookCommand = new RelayCommand(_ => _ = BrowseWorkbookAsync());
            AutoMatchCommand = new RelayCommand(_ => AutoMatchMappings());
            SaveMappingCommand = new RelayCommand(_ => _ = SaveMappingAsync());
            ImportCommand = new RelayCommand(_ => _ = ImportAsync());

            Replace(Targets, _coordinator.GetExcelImportTargets());
            SelectedTarget = Targets.FirstOrDefault();
            RefreshHeaderOptions();
        }

        public void UpdateRuntimeData(IReadOnlyCollection<AppConstantDto> appConstants)
        {
            _appConstants = appConstants;
            _localMappings.Clear();
            RebuildMappingRows();
        }

        private Task BrowseWorkbookAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel workbook|*.xlsx",
                Multiselect = false,
                Title = "Choose Excel workbook"
            };

            if (dialog.ShowDialog() != true)
            {
                return Task.CompletedTask;
            }

            try
            {
                IsBusy = true;
                WorkbookPath = dialog.FileName;

                var workbook = _coordinator.ReadExcelWorkbook(dialog.FileName);
                Replace(WorkbookHeaders, workbook.Headers);
                RefreshHeaderOptions();
                WorkbookSummary = $"Detected {workbook.Headers.Count} header(s) and {workbook.RowCount} data row(s) in the first worksheet.";
                RebuildMappingRows();
                SetStatus("Workbook loaded. Review the suggested mappings before importing.", true);
            }
            catch (Exception ex)
            {
                Replace(WorkbookHeaders, Array.Empty<string>());
                RefreshHeaderOptions();
                WorkbookSummary = "Workbook could not be read.";
                SetStatus(ex.Message, false);
            }
            finally
            {
                IsBusy = false;
            }

            return Task.CompletedTask;
        }

        private void AutoMatchMappings()
        {
            if (SelectedTarget == null)
            {
                SetStatus("Choose a table first.", false);
                return;
            }

            ApplyResolvedMappings(
                _coordinator.ResolveExcelImportMappings(
                    SelectedTarget.Key,
                    WorkbookHeaders.ToList(),
                    _appConstants,
                    GetSavedMappingsForSelectedTarget()));

            SetStatus("Header mapping suggestions refreshed.", true);
        }

        private async Task SaveMappingAsync()
        {
            if (SelectedTarget == null)
            {
                SetStatus("Choose a table first.", false);
                return;
            }

            try
            {
                IsBusy = true;

                var mappings = BuildCurrentMappings();
                await _coordinator.SaveExcelImportProfileAsync(SelectedTarget.Key, mappings);
                _localMappings[SelectedTarget.Key] = new Dictionary<string, string?>(mappings, StringComparer.OrdinalIgnoreCase);
                SetStatus($"Saved Excel mapping for {SelectedTarget.TableName}.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Saving mapping failed: {ex.Message}", false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ImportAsync()
        {
            if (SelectedTarget == null)
            {
                SetStatus("Choose a table first.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(WorkbookPath))
            {
                SetStatus("Choose an Excel workbook first.", false);
                return;
            }

            try
            {
                IsBusy = true;

                var result = await _coordinator.ImportFromExcelAsync(
                    SelectedTarget.Key,
                    WorkbookPath,
                    _contextFactory(),
                    BuildCurrentMappings(),
                    _appConstants);

                SetStatus(result.SummaryMessage, result.HasImportedRows);

                CustomMessageBox.Show(
                    result.ToDialogMessage(),
                    "Excel Import",
                    result.HasErrors
                        ? (result.HasImportedRows ? "Warning" : "Error")
                        : "Success");

                if (result.HasImportedRows)
                {
                    await _refreshAllAsync();
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Import failed: {ex.Message}", false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RebuildMappingRows()
        {
            if (SelectedTarget == null)
            {
                Replace(MappingRows, Array.Empty<ExcelImportFieldMappingRow>());
                return;
            }

            var resolvedMappings = _coordinator.ResolveExcelImportMappings(
                SelectedTarget.Key,
                WorkbookHeaders.ToList(),
                _appConstants,
                GetSavedMappingsForSelectedTarget());

            var rows = _coordinator.GetExcelImportFields(SelectedTarget.Key)
                .Select(field => new ExcelImportFieldMappingRow
                {
                    FieldKey = field.Key,
                    ColumnName = field.ColumnName,
                    Description = field.Description,
                    DataTypeLabel = field.DataTypeLabel,
                    IsRequired = field.IsRequired,
                    SelectedHeader = resolvedMappings.TryGetValue(field.Key, out var header)
                        && !string.IsNullOrWhiteSpace(header)
                            ? header
                            : NotMappedHeader
                });

            Replace(MappingRows, rows);
        }

        private void ApplyResolvedMappings(IReadOnlyDictionary<string, string?> mappings)
        {
            foreach (var row in MappingRows)
            {
                row.SelectedHeader = mappings.TryGetValue(row.FieldKey, out var header)
                    && !string.IsNullOrWhiteSpace(header)
                        ? header
                        : NotMappedHeader;
            }
        }

        private IReadOnlyDictionary<string, string?> BuildCurrentMappings()
            => MappingRows.ToDictionary(
                row => row.FieldKey,
                row => string.Equals(row.SelectedHeader, NotMappedHeader, StringComparison.Ordinal)
                    ? null
                    : row.SelectedHeader,
                StringComparer.OrdinalIgnoreCase);

        private IReadOnlyDictionary<string, string?>? GetSavedMappingsForSelectedTarget()
            => SelectedTarget != null && _localMappings.TryGetValue(SelectedTarget.Key, out var mappings)
                ? mappings
                : null;

        private void RefreshHeaderOptions()
            => Replace(WorkbookHeaderOptions, new[] { NotMappedHeader }.Concat(WorkbookHeaders));

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            _statusReporter(message, isSuccess);
        }

        private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
