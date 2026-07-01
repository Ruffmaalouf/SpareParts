using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class RepairPrepBoardViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private RepairPrepCarRow? _selectedCar;
        private string _filterText = string.Empty;
        private string _status = "Load used cars to plan repair and listing prep.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;
        private string _newTaskTitle = string.Empty;
        private decimal _newTaskCost;

        public RepairPrepBoardViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshCommand = LoadCommand;
            MoveSelectedToStatusCommand = new RelayCommand(MoveSelectedToStatus);
            AddTaskCommand = new RelayCommand(_ => AddTask());
            DeleteTaskCommand = new RelayCommand(DeleteTask);

            foreach (var column in RepairPrepColumn.CreateDefaultColumns())
            {
                Columns.Add(column);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<RepairPrepColumn> Columns { get; } = new();
        public ObservableCollection<RepairPrepCarRow> Cars { get; } = new();
        public ObservableCollection<RepairPrepTaskRow> SelectedTasks { get; } = new();
        public ObservableCollection<RepairPrepLinkedPartRow> SelectedLinkedParts { get; } = new();
        public ObservableCollection<RepairPrepMetricTile> MetricTiles { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand MoveSelectedToStatusCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText == value)
                {
                    return;
                }

                _filterText = value ?? string.Empty;
                OnPropertyChanged(nameof(FilterText));
                RefreshColumns();
            }
        }

        public RepairPrepCarRow? SelectedCar
        {
            get => _selectedCar;
            set
            {
                if (ReferenceEquals(_selectedCar, value))
                {
                    return;
                }

                _selectedCar = value;
                OnPropertyChanged(nameof(SelectedCar));
                OnPropertyChanged(nameof(SelectedCarTitle));
                OnPropertyChanged(nameof(SelectedCarSubtitle));
                OnPropertyChanged(nameof(SelectedProgressLabel));
                OnPropertyChanged(nameof(SelectedPrepCostLabel));
                RefreshSelectedDetails();
            }
        }

        public string SelectedCarTitle => SelectedCar?.Title ?? "Select a used car";

        public string SelectedCarSubtitle => SelectedCar == null
            ? "Pick a car from the board to inspect tasks, costs, and linked parts."
            : $"{SelectedCar.StatusLabel} - {SelectedCar.SupplierDisplay} - {SelectedCar.LocationDisplay}";

        public string SelectedProgressLabel => SelectedCar == null
            ? "No task progress yet."
            : $"{SelectedCar.CompletedTaskCount:N0}/{SelectedCar.TaskCount:N0} tasks complete ({SelectedCar.ProgressPercent:N0}%).";

        public string SelectedPrepCostLabel => SelectedCar == null
            ? "Prep cost USD 0.00"
            : $"Prep tasks {SelectedCar.Currency} {SelectedCar.TaskCost:N2}";

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

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value)
                {
                    return;
                }

                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string NewTaskTitle
        {
            get => _newTaskTitle;
            set
            {
                if (_newTaskTitle == value)
                {
                    return;
                }

                _newTaskTitle = value ?? string.Empty;
                OnPropertyChanged(nameof(NewTaskTitle));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public decimal NewTaskCost
        {
            get => _newTaskCost;
            set
            {
                if (_newTaskCost == value)
                {
                    return;
                }

                _newTaskCost = value;
                OnPropertyChanged(nameof(NewTaskCost));
            }
        }

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            Status = "Loading repair/prep board...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var carsTask = _crudApi.GetAllAsync<UsedCarDto>("/api/usedcars");
                var partsTask = _crudApi.GetAllAsync<PartDto>("/api/parts?page=1&pageSize=5000");
                await Task.WhenAll(carsTask, partsTask);

                var partsByCar = partsTask.Result
                    .Where(part => part.UsedCarId.HasValue)
                    .GroupBy(part => part.UsedCarId!.Value)
                    .ToDictionary(group => group.Key, group => group.ToList());
                var rows = carsTask.Result
                    .Select(car => RepairPrepCarRow.From(car, partsByCar.TryGetValue(car.Id, out var parts) ? parts : new List<PartDto>()))
                    .OrderBy(row => row.StatusSortOrder)
                    .ThenByDescending(row => row.FullCost)
                    .ToList();

                Replace(Cars, rows);
                RefreshColumns(selectFirstWhenMissing: true);
                RebuildMetrics();

                Status = rows.Count == 0
                    ? "No used cars returned for repair/prep."
                    : $"Repair/prep board loaded with {rows.Count:N0} car(s).";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                Status = "Could not load repair/prep board.";
                StatusBrush = Brushes.IndianRed;
                AppNotificationCenter.Instance.Publish($"Repair/prep board failed: {ex.Message}", false);
                Cars.Clear();
                foreach (var column in Columns)
                {
                    column.Cars.Clear();
                }
                MetricTiles.Clear();
                SelectedCar = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void MoveSelectedToStatus(object? parameter)
        {
            if (SelectedCar == null || parameter is not string statusKey)
            {
                return;
            }

            var column = Columns.FirstOrDefault(item => string.Equals(item.Key, statusKey, StringComparison.OrdinalIgnoreCase));
            if (column == null)
            {
                return;
            }

            SelectedCar.StatusKey = column.Key;
            SelectedCar.StatusLabel = column.Label;
            SelectedCar.StatusSortOrder = column.SortOrder;
            RefreshColumns();
            RebuildMetrics();
            OnPropertyChanged(nameof(SelectedCarSubtitle));
        }

        private void AddTask()
        {
            if (SelectedCar == null || string.IsNullOrWhiteSpace(NewTaskTitle))
            {
                return;
            }

            var task = new RepairPrepTaskRow(NewTaskTitle.Trim(), Math.Max(0, NewTaskCost));
            task.PropertyChanged += (_, _) => RefreshSelectedTaskSummary();
            SelectedCar.Tasks.Add(task);
            SelectedTasks.Add(task);
            NewTaskTitle = string.Empty;
            NewTaskCost = 0m;
            RefreshSelectedTaskSummary();
        }

        private void DeleteTask(object? parameter)
        {
            if (SelectedCar == null || parameter is not RepairPrepTaskRow task)
            {
                return;
            }

            SelectedCar.Tasks.Remove(task);
            SelectedTasks.Remove(task);
            RefreshSelectedTaskSummary();
        }

        private void RefreshColumns(bool selectFirstWhenMissing = false)
        {
            var query = Normalize(FilterText);
            foreach (var column in Columns)
            {
                column.Cars.Clear();
            }

            var rows = string.IsNullOrWhiteSpace(query)
                ? Cars.ToList()
                : Cars.Where(car => car.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var car in rows)
            {
                var column = Columns.FirstOrDefault(item => item.Key == car.StatusKey) ?? Columns[0];
                column.Cars.Add(car);
            }

            foreach (var column in Columns)
            {
                column.OnCarsChanged();
            }

            if (selectFirstWhenMissing && (SelectedCar == null || !Cars.Contains(SelectedCar)))
            {
                SelectedCar = rows.FirstOrDefault();
            }
        }

        private void RefreshSelectedDetails()
        {
            SelectedTasks.Clear();
            SelectedLinkedParts.Clear();

            if (SelectedCar == null)
            {
                return;
            }

            foreach (var task in SelectedCar.Tasks)
            {
                task.PropertyChanged += (_, _) => RefreshSelectedTaskSummary();
                SelectedTasks.Add(task);
            }

            foreach (var part in SelectedCar.LinkedParts)
            {
                SelectedLinkedParts.Add(part);
            }
        }

        private void RefreshSelectedTaskSummary()
        {
            SelectedCar?.RefreshTaskSummary();
            RebuildMetrics();
            OnPropertyChanged(nameof(SelectedProgressLabel));
            OnPropertyChanged(nameof(SelectedPrepCostLabel));
        }

        private void RebuildMetrics()
        {
            var active = Cars.Count(car => car.StatusKey is not "listed" and not "sold");
            var doneTasks = Cars.Sum(car => car.CompletedTaskCount);
            var totalTasks = Cars.Sum(car => car.TaskCount);

            MetricTiles.Clear();
            MetricTiles.Add(new RepairPrepMetricTile("Cars", Cars.Count.ToString("N0", CultureInfo.CurrentCulture), "Total used cars", Brushes.LightSkyBlue));
            MetricTiles.Add(new RepairPrepMetricTile("Active", active.ToString("N0", CultureInfo.CurrentCulture), "Before listing or sale", Brushes.Gold));
            MetricTiles.Add(new RepairPrepMetricTile("Tasks", $"{doneTasks:N0}/{totalTasks:N0}", "Prep checklist", Brushes.LightGreen));
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string Normalize(string? value)
            => string.Join(" ", (value ?? string.Empty)
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        private void HandleBackgroundException(Exception ex)
        {
            Status = ex.Message;
            StatusBrush = Brushes.IndianRed;
            AppNotificationCenter.Instance.Publish(ex.Message, false);
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
