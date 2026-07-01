using SpareParts.Desktop.Wpf.Management;
using SpareParts.Domain.Inventory;
using Microsoft.Win32;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.UsedCars;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class UsedCarPartsWindow : Window, INotifyPropertyChanged
    {
        private readonly ManagementCoordinator _coordinator;
        private readonly UsedCarWorkspaceRequest _request;
        private readonly IFilePickerService _filePickerService;
        private readonly IUserNotificationService _notificationService;
        private readonly List<PartDto> _allParts = new();
        private PartDto? _selectedAvailablePart;
        private PartDto? _selectedAssignedPart;
        private string _searchText = string.Empty;
        private string _footerStatus = "Loading parts...";
        private bool _isBusy;

        public UsedCarPartsWindow(
            ManagementCoordinator coordinator,
            UsedCarWorkspaceRequest request,
            IFilePickerService filePickerService,
            IUserNotificationService notificationService)
        {
            InitializeComponent();
            _coordinator = coordinator;
            _request = request;
            _filePickerService = filePickerService;
            _notificationService = notificationService;
            DataContext = this;

            Loaded += async (_, _) => await LoadPartsAsync();
        }

        public ObservableCollection<PartDto> AvailableParts { get; } = new();

        public ObservableCollection<PartDto> AssignedParts { get; } = new();

        public ObservableCollection<UsedCarPartRecommendation> RecommendedParts { get; } = new();

        public string WindowTitle => $"Used Car Parts - {_request.UsedCarName}";

        public string WindowSubtitle => $"Used car #{_request.UsedCarId} - manage linked parts from the main parts table";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplyPartsFilter();
            }
        }

        public PartDto? SelectedAvailablePart
        {
            get => _selectedAvailablePart;
            set
            {
                if (_selectedAvailablePart == value)
                {
                    return;
                }

                _selectedAvailablePart = value;
                OnPropertyChanged(nameof(SelectedAvailablePart));
                OnPropertyChanged(nameof(CanAssignPart));
                OnPropertyChanged(nameof(SelectedPartSummary));
            }
        }

        public PartDto? SelectedAssignedPart
        {
            get => _selectedAssignedPart;
            set
            {
                if (_selectedAssignedPart == value)
                {
                    return;
                }

                _selectedAssignedPart = value;
                OnPropertyChanged(nameof(SelectedAssignedPart));
                OnPropertyChanged(nameof(CanRemovePart));
                OnPropertyChanged(nameof(SelectedPartSummary));
            }
        }

        public bool HasAvailableParts => AvailableParts.Count > 0;

        public bool HasAssignedParts => AssignedParts.Count > 0;

        public bool HasRecommendedParts => RecommendedParts.Count > 0;

        public bool CanAssignPart => !_isBusy && SelectedAvailablePart is { Id: > 0 };

        public bool CanRemovePart => !_isBusy && SelectedAssignedPart is { Id: > 0 };

        public bool CanImportParts => !_isBusy;

        public string AvailableSummary => $"{AvailableParts.Count} unassigned part(s) available to link.";

        public string AssignedSummary => $"{AssignedParts.Count} part(s) currently linked to this used car.";

        public string RecommendationSummary => HasRecommendedParts
            ? $"{RecommendedParts.Count} high-value next part(s) from the current list."
            : "No stocked parts match the current list.";

        public string SearchSummary => string.IsNullOrWhiteSpace(SearchText)
            ? "Search by code, name, or OEM number."
            : $"Filtered by \"{SearchText.Trim()}\".";

        public string SelectedPartSummary
        {
            get
            {
                if (SelectedAvailablePart != null)
                {
                    return $"Ready to assign: {SelectedAvailablePart.InternalCode} - {SelectedAvailablePart.Name}";
                }

                if (SelectedAssignedPart != null)
                {
                    return $"Ready to remove: {SelectedAssignedPart.InternalCode} - {SelectedAssignedPart.Name}";
                }

                return "Select a part from either list to assign or remove it.";
            }
        }

        public string FooterStatus
        {
            get => _footerStatus;
            private set
            {
                if (_footerStatus == value)
                {
                    return;
                }

                _footerStatus = value;
                OnPropertyChanged(nameof(FooterStatus));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadPartsAsync();
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            var filePath = _filePickerService
                .PickFiles(new FilePickerRequest
                {
                    Filter = "Excel workbook|*.xlsx",
                    AllowMultiple = false,
                    Title = "Import Used Parts From Excel"
                })
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            try
            {
                SetBusy(true, "Importing parts from Excel...");

                var result = await _coordinator.ImportPartsFromExcelAsync(filePath);

                if (result.HasImportedRows)
                {
                    await LoadPartsAsync();
                }

                FooterStatus = result.SummaryMessage;
                _notificationService.Show(
                    result.ToDialogMessage(),
                    "Used Car Parts",
                    result.HasErrors ? NotificationKind.Warning : NotificationKind.Success);
            }
            catch (Exception ex)
            {
                FooterStatus = "Import failed.";
                _notificationService.Show(ex.Message, "Used Car Parts", NotificationKind.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void AssignSelected_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAvailablePart is not { Id: > 0 } part)
            {
                _notificationService.Show("Select an available part first.", "Used Car Parts", NotificationKind.Warning);
                return;
            }

            try
            {
                SetBusy(true, "Assigning part...");
                await _coordinator.SetPartUsedCarAsync(part.Id, _request.UsedCarId);
                await LoadPartsAsync();
                FooterStatus = $"Assigned part {part.InternalCode} to used car #{_request.UsedCarId}.";
            }
            catch (Exception ex)
            {
                FooterStatus = "Assign failed.";
                _notificationService.Show(ex.Message, "Used Car Parts", NotificationKind.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAssignedPart is not { Id: > 0 } part)
            {
                _notificationService.Show("Select an assigned part first.", "Used Car Parts", NotificationKind.Warning);
                return;
            }

            try
            {
                SetBusy(true, "Removing part...");
                await _coordinator.SetPartUsedCarAsync(part.Id, null);
                await LoadPartsAsync();
                FooterStatus = $"Removed part {part.InternalCode} from used car #{_request.UsedCarId}.";
            }
            catch (Exception ex)
            {
                FooterStatus = "Remove failed.";
                _notificationService.Show(ex.Message, "Used Car Parts", NotificationKind.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectRecommendation_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UsedCarPartRecommendation recommendation)
            {
                return;
            }

            if (recommendation.IsAssigned)
            {
                SelectedAssignedPart = AssignedParts.FirstOrDefault(part => part.Id == recommendation.PartId);
                SelectedAvailablePart = null;
                return;
            }

            SelectedAvailablePart = AvailableParts.FirstOrDefault(part => part.Id == recommendation.PartId);
            SelectedAssignedPart = null;
        }

        private async Task LoadPartsAsync()
        {
            try
            {
                SetBusy(true, "Refreshing linked parts...");

                var parts = await _coordinator.GetAllPartsAsync();

                _allParts.Clear();
                _allParts.AddRange(parts
                    .OrderBy(part => part.InternalCode, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(part => part.Name, StringComparer.OrdinalIgnoreCase));

                ApplyPartsFilter();
                FooterStatus = "Parts ready.";
            }
            catch (Exception ex)
            {
                FooterStatus = "Unable to load parts.";
                _notificationService.Show(ex.Message, "Used Car Parts", NotificationKind.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ApplyPartsFilter()
        {
            var filtered = _allParts.Where(MatchesSearch).ToList();

            ReplaceCollection(
                AvailableParts,
                filtered.Where(part => part.UsedCarId == null));

            ReplaceCollection(
                AssignedParts,
                filtered.Where(part => part.UsedCarId == _request.UsedCarId));

            if (SelectedAvailablePart is not null && AvailableParts.All(part => part.Id != SelectedAvailablePart.Id))
            {
                SelectedAvailablePart = null;
            }

            if (SelectedAssignedPart is not null && AssignedParts.All(part => part.Id != SelectedAssignedPart.Id))
            {
                SelectedAssignedPart = null;
            }

            OnPropertyChanged(nameof(HasAvailableParts));
            OnPropertyChanged(nameof(HasAssignedParts));
            OnPropertyChanged(nameof(AvailableSummary));
            OnPropertyChanged(nameof(AssignedSummary));
            OnPropertyChanged(nameof(SearchSummary));
            RefreshRecommendations();
        }

        private void RefreshRecommendations()
        {
            var recommendations = AssignedParts
                .Select(part => UsedCarPartRecommendation.FromPart(part, isAssigned: true))
                .Concat(AvailableParts.Select(part => UsedCarPartRecommendation.FromPart(part, isAssigned: false)))
                .Where(recommendation => recommendation.QuantityAvailable > 0 && recommendation.SalePrice > 0m)
                .OrderByDescending(recommendation => recommendation.Score)
                .ThenByDescending(recommendation => recommendation.MarginValue)
                .ThenBy(recommendation => recommendation.Name, StringComparer.OrdinalIgnoreCase)
                .Take(3);

            ReplaceCollection(RecommendedParts, recommendations);

            OnPropertyChanged(nameof(HasRecommendedParts));
            OnPropertyChanged(nameof(RecommendationSummary));
        }

        private bool MatchesSearch(PartDto part)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var term = SearchText.Trim();

            return Contains(part.InternalCode, term)
                || Contains(part.Name, term)
                || Contains(part.OEMNumber, term);
        }

        private static bool Contains(string? source, string term)
            => !string.IsNullOrWhiteSpace(source)
                && source.Contains(term, StringComparison.OrdinalIgnoreCase);

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private void SetBusy(bool isBusy, string? busyMessage = null)
        {
            _isBusy = isBusy;
            OnPropertyChanged(nameof(CanImportParts));
            OnPropertyChanged(nameof(CanAssignPart));
            OnPropertyChanged(nameof(CanRemovePart));

            if (isBusy && !string.IsNullOrWhiteSpace(busyMessage))
            {
                FooterStatus = busyMessage;
            }
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
