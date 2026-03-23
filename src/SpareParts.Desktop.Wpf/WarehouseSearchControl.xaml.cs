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
        private List<WarehouseDto> _all = new();
        private bool _suppressClose;

        // ── Dependency Properties ─────────────────────────────────────────────

        public static readonly DependencyProperty SelectedWarehouseIdProperty =
            DependencyProperty.Register(nameof(SelectedWarehouseId), typeof(int?),
                typeof(WarehouseSearchControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int? SelectedWarehouseId
        {
            get => (int?)GetValue(SelectedWarehouseIdProperty);
            set => SetValue(SelectedWarehouseIdProperty, value);
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
                new PropertyMetadata(new ObservableCollection<WarehouseDto>()));

        public ObservableCollection<WarehouseDto> FilteredWarehouses
        {
            get => (ObservableCollection<WarehouseDto>)GetValue(FilteredWarehousesProperty);
            set => SetValue(FilteredWarehousesProperty, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public WarehouseSearchControl()
        {
            InitializeComponent();
            FilteredWarehouses = new ObservableCollection<WarehouseDto>();
            Loaded += async (_, _) => await EnsureLoadedAsync();

            Application.Current.MainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
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
                d = VisualTreeHelper.GetParent(d) ??
                    LogicalTreeHelper.GetParent(d) as DependencyObject;
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
            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            _ = OpenPopupAsync();
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
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

        private void ResultItem_DoubleClick(object sender, MouseButtonEventArgs e)
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
                _all = await ApiClient.Instance.GetAllAsync<WarehouseDto>("api/warehouses");
            }
            catch
            {
                _all = new List<WarehouseDto> { new() { Id = 1, Name = "Main Warehouse", IsMain = true } };
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
                : _all.Where(w => w.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            FilteredWarehouses.Clear();
            foreach (var w in results) FilteredWarehouses.Add(w);

            if (FilteredWarehouses.Count > 0) ResultsPopup.IsOpen = true;
        }

        private void Select(WarehouseDto w)
        {
            SelectedWarehouseId          = w.Id;
            SelectedNameInline.Text      = w.Name + (w.IsMain ? " ★" : "");
            SelectedIndicator.Visibility = Visibility.Visible;
            WSearchIcon.Visibility       = Visibility.Collapsed;
            SearchBtn.Visibility         = Visibility.Collapsed;
            ClearBtn.Visibility          = Visibility.Visible;

            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            WarehouseSearchText       = string.Empty;
            SearchBox.Text            = string.Empty;
            ShowPlaceholder();
            ClosePopup();
        }

        private void Clear()
        {
            SelectedWarehouseId          = null;
            SelectedIndicator.Visibility = Visibility.Collapsed;
            WSearchIcon.Visibility       = Visibility.Visible;
            SelectedNameInline.Text      = string.Empty;
            SearchBtn.Visibility         = Visibility.Visible;
            ClearBtn.Visibility          = Visibility.Collapsed;
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
            InputPill.BorderBrush     = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
