using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public partial class WarehouseSearchControl : UserControl
    {

        private readonly ICrudApiClient _crudApi;
        private List<WarehouseDto> _all = new();
        private bool _suppressClose;

        // ── Dependency Properties ─────────────────────────────────────────────

        public static readonly DependencyProperty SelectedWarehouseIdProperty =
            DependencyProperty.Register(nameof(SelectedWarehouseId), typeof(int?),
                typeof(WarehouseSearchControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedWarehouseIdChanged));

        public int? SelectedWarehouseId
        {
            get => (int?)GetValue(SelectedWarehouseIdProperty);
            set => SetValue(SelectedWarehouseIdProperty, value);
        }

        private static void OnSelectedWarehouseIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not WarehouseSearchControl control)
            {
                return;
            }

            var value = e.NewValue as int?;
            if (e.NewValue is int id)
            {
                value = id;
            }

            _ = control.ApplyExternalSelectionAsync(value);
        }

        public static readonly DependencyProperty WarehouseSearchTextProperty =
            DependencyProperty.Register(nameof(WarehouseSearchText), typeof(string),
                typeof(WarehouseSearchControl), new PropertyMetadata(string.Empty));

        public string WarehouseSearchText
        {
            get => (string)GetValue(WarehouseSearchTextProperty);
            set => SetValue(WarehouseSearchTextProperty, value);
        }

        public static readonly DependencyProperty FilteredWarehousesProperty =
            DependencyProperty.Register(nameof(FilteredWarehouses),
                typeof(ObservableCollection<WarehouseDto>), typeof(WarehouseSearchControl),
                new PropertyMetadata(null));

        public ObservableCollection<WarehouseDto> FilteredWarehouses
        {
            get => (ObservableCollection<WarehouseDto>)GetValue(FilteredWarehousesProperty);
            set => SetValue(FilteredWarehousesProperty, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public WarehouseSearchControl() : this(ServiceLocator.Resolve<ICrudApiClient>())
        {
        }

        public WarehouseSearchControl(ICrudApiClient crudApi)
        {
            InitializeComponent();
            _crudApi = crudApi;
            FilteredWarehouses = new ObservableCollection<WarehouseDto>();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await EnsureLoadedAsync();
            AttachMainWindowHandler();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachMainWindowHandler();
        }

        private void AttachMainWindowHandler()
        {
            if (Application.Current?.MainWindow == null)
            {
                return;
            }

            Application.Current.MainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
            Application.Current.MainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
        }

        private void DetachMainWindowHandler()
        {
            if (Application.Current?.MainWindow == null)
            {
                return;
            }

            Application.Current.MainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ResultsPopup.IsOpen) return;
            if (_suppressClose) { _suppressClose = false; return; }

            var hit = e.OriginalSource as DependencyObject;
            if (IsInsideControl(hit)) return;
            ClosePopup();
        }

        private bool IsInsideControl(DependencyObject? d)
        {
            while (d != null)
            {
                if (d == InputPill || d == ResultsPopup) return true;
                if (d is System.Windows.Controls.Primitives.Popup) return true;
                d = SearchTreeHelper.GetParent(d);
            }
            return false;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _suppressClose = true;
            _ = OpenPopupAsync();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    var exact = FindExactScanMatch();
                    if (exact != null) { Select(exact); return; }
                    if (FilteredWarehouses.Count == 1) { Select(FilteredWarehouses[0]); return; }
                    _ = OpenPopupAsync();
                    break;
                case Key.Escape:
                    ClosePopup();
                    RestorePill();
                    break;
                case Key.Down when ResultsPopup.IsOpen:
                    _suppressClose = true;
                    ResultsList.Focus();
                    if (ResultsList.Items.Count > 0) ResultsList.SelectedIndex = 0;
                    break;
                default:
                    if (_all.Count > 0) ApplyFilter();
                    break;
            }
        }

        private void ResultItem_Click(object sender, MouseButtonEventArgs e)
        {
            _suppressClose = true;
            if (ResultsList.SelectedItem is WarehouseDto w) Select(w);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is WarehouseDto w) Select(w);
            else if (e.Key == Key.Escape) { ClosePopup(); SearchBox.Focus(); }
        }

        private void ClearWarehouse_Click(object sender, RoutedEventArgs e) => Clear();

        // ── Logic ─────────────────────────────────────────────────────────────

        private async Task EnsureLoadedAsync()
        {
            if (_all.Count > 0) return;
            try
            {
                _all = await _crudApi.GetAllAsync<WarehouseDto>("api/warehouses");
                SetLoadError(false);
                await ApplyExternalSelectionAsync(SelectedWarehouseId);
            }
            catch (Exception ex)
            {
                // Do not inject placeholder warehouses; it can lead to invalid selections and wrong transactions.
                _all = new List<WarehouseDto>();
                FilteredWarehouses.Clear();
                ClosePopup();
                SelectedWarehouseId = null;
                System.Diagnostics.Debug.WriteLine($"[WarehouseSearch] load failed: {ex.Message}");
                SetLoadError(true);
                CustomMessageBox.Show(
                    $"Could not load warehouses: {ex.Message}",
                    "Warehouse Search",
                    "Error");
            }
        }

        private void SetLoadError(bool hasError)
        {
            if (hasError)
            {
                EmptyStateText.Text = "Couldn't load warehouses. Check your connection and try again.";
                EmptyState.Visibility = Visibility.Visible;
                ResultsList.Visibility = Visibility.Collapsed;
                ResultsPopup.IsOpen = true;
            }
            else
            {
                EmptyStateText.Text = "No warehouses found";
                EmptyState.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ApplyExternalSelectionAsync(int? warehouseId)
        {
            if (!warehouseId.HasValue || warehouseId.Value <= 0)
            {
                SelectedIndicator.Visibility = Visibility.Collapsed;
                WSearchIcon.Visibility = Visibility.Visible;
                SelectedNameInline.Text = string.Empty;
                SearchBtn.Visibility = Visibility.Visible;
                ClearBtn.Visibility = Visibility.Collapsed;
                return;
            }

            if (_all.Count == 0)
            {
                await EnsureLoadedAsync();
            }

            var selected = _all.FirstOrDefault(w => w.Id == warehouseId.Value);
            if (selected != null)
            {
                Select(selected);
            }
        }

        private async Task OpenPopupAsync()
        {
            await EnsureLoadedAsync();
            ApplyFilter();
            ResultsPopup.IsOpen = true;

            if (FilteredWarehouses.Count > 0)
            {
                _suppressClose = true;
                ResultsList.Focus();
                ResultsList.SelectedIndex = 0;
            }
        }

        private void ApplyFilter()
        {
            var q = (WarehouseSearchText ?? "").Trim();
            var results = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(w =>
                    w.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (w.Barcode ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            FilteredWarehouses.Clear();
            foreach (var w in results) FilteredWarehouses.Add(w);

            if (FilteredWarehouses.Count > 0) ResultsPopup.IsOpen = true;
        }

        private WarehouseDto? FindExactScanMatch()
        {
            var q = (WarehouseSearchText ?? string.Empty).Trim();
            if (q.Length == 0)
            {
                return null;
            }

            return _all.FirstOrDefault(w =>
                string.Equals(w.Barcode, q, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(w.Name, q, StringComparison.OrdinalIgnoreCase));
        }

        private void Select(WarehouseDto w)
        {
            SelectedWarehouseId = w.Id;
            SelectedNameInline.Text = w.Name + (w.IsMain ? " ★" : "");
            SelectedIndicator.Visibility = Visibility.Visible;
            WSearchIcon.Visibility = Visibility.Collapsed;
            SearchBtn.Visibility = Visibility.Collapsed;
            ClearBtn.Visibility = Visibility.Visible;

            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            WarehouseSearchText = string.Empty;
            SearchBox.Text = string.Empty;
            HidePlaceholder();
            ClosePopup();
        }

        private void Clear()
        {
            SelectedWarehouseId = null;
            SelectedIndicator.Visibility = Visibility.Collapsed;
            WSearchIcon.Visibility = Visibility.Visible;
            SelectedNameInline.Text = string.Empty;
            SearchBtn.Visibility = Visibility.Visible;
            ClearBtn.Visibility = Visibility.Collapsed;
            RestorePill();
            ShowPlaceholder();
            SearchBox.Focus();
        }

        private void ClosePopup()
        {
            ResultsPopup.IsOpen = false;
            _suppressClose = false;
        }

        private void ShowPlaceholder() => Placeholder.Visibility = Visibility.Visible;
        private void HidePlaceholder() => Placeholder.Visibility = Visibility.Collapsed;

        private void RestorePill()
        {
            InputPill.BorderBrush = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
