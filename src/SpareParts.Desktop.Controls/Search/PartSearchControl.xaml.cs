using SpareParts.Domain.Inventory;
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
    public partial class PartSearchControl : UserControl
    {

        private readonly IPartsApiClient _partsApi;
        private List<PartDto> _allParts = new();
        private bool _suppressClose;

        // ── Dependency Properties ─────────────────────────────────────────────

        public static readonly DependencyProperty SelectedPartIdProperty =
            DependencyProperty.Register(
                nameof(SelectedPartId), typeof(int?), typeof(PartSearchControl),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedPartIdChanged));

        public int? SelectedPartId
        {
            get => (int?)GetValue(SelectedPartIdProperty);
            set => SetValue(SelectedPartIdProperty, value);
        }

        private static void OnSelectedPartIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PartSearchControl control) return;
            var value = e.NewValue as int?;
            if (e.NewValue is int id)
            {
                value = id;
            }

            if (!value.HasValue || value.Value <= 0)
            {
                control.ClearSelectionVisualState();
                return;
            }

            _ = control.ApplyExternalSelectionAsync(value);
        }

        public static readonly DependencyProperty SelectedPartPriceProperty =
            DependencyProperty.Register(
                nameof(SelectedPartPrice), typeof(decimal), typeof(PartSearchControl),
                new FrameworkPropertyMetadata(0m,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public decimal SelectedPartPrice
        {
            get => (decimal)GetValue(SelectedPartPriceProperty);
            set => SetValue(SelectedPartPriceProperty, value);
        }

        public static readonly DependencyProperty SelectedPartCostPriceProperty =
            DependencyProperty.Register(
                nameof(SelectedPartCostPrice), typeof(decimal), typeof(PartSearchControl),
                new FrameworkPropertyMetadata(0m,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public decimal SelectedPartCostPrice
        {
            get => (decimal)GetValue(SelectedPartCostPriceProperty);
            set => SetValue(SelectedPartCostPriceProperty, value);
        }

        /// <summary>Bound to InvoiceTabViewModel.NewPartDescription so the grid shows the name.</summary>
        public static readonly DependencyProperty SelectedPartDescriptionProperty =
            DependencyProperty.Register(
                nameof(SelectedPartDescription), typeof(string), typeof(PartSearchControl),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string SelectedPartDescription
        {
            get => (string)GetValue(SelectedPartDescriptionProperty);
            set => SetValue(SelectedPartDescriptionProperty, value);
        }

        public static readonly DependencyProperty PartSearchTextProperty =
            DependencyProperty.Register(nameof(PartSearchText), typeof(string),
                typeof(PartSearchControl), new PropertyMetadata(string.Empty));

        public string PartSearchText
        {
            get => (string)GetValue(PartSearchTextProperty);
            set => SetValue(PartSearchTextProperty, value);
        }

        public static readonly DependencyProperty FilteredPartsProperty =
            DependencyProperty.Register(nameof(FilteredParts),
                typeof(ObservableCollection<PartDto>), typeof(PartSearchControl),
                new PropertyMetadata(null));

        public ObservableCollection<PartDto> FilteredParts
        {
            get => (ObservableCollection<PartDto>)GetValue(FilteredPartsProperty);
            set => SetValue(FilteredPartsProperty, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public PartSearchControl() : this(ServiceLocator.Resolve<IPartsApiClient>())
        {
        }

        public PartSearchControl(IPartsApiClient partsApi)
        {
            InitializeComponent();
            _partsApi = partsApi;
            FilteredParts = new ObservableCollection<PartDto>();
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
            _ = LoadAndFilterAsync();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Intentionally empty — popup is closed by MainWindow_PreviewMouseDown
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    var exact = FindExactScanMatch();
                    if (exact != null) { SelectPart(exact); return; }
                    if (FilteredParts.Count == 1) { SelectPart(FilteredParts[0]); return; }
                    _ = LoadAndFilterAsync();
                    break;
                case Key.Escape:
                    ClosePopup();
                    RestorePillBorder();
                    break;
                case Key.Down when ResultsPopup.IsOpen:
                    _suppressClose = true;
                    ResultsList.Focus();
                    if (ResultsList.Items.Count > 0) ResultsList.SelectedIndex = 0;
                    break;
                default:
                    HidePlaceholder();
                    if (string.IsNullOrEmpty(SearchBox.Text)) ShowPlaceholder();
                    if (_allParts.Count > 0) ApplyFilter();
                    break;
            }
        }

        private void ResultItem_Click(object sender, MouseButtonEventArgs e)
        {
            _suppressClose = true;
            if (ResultsList.SelectedItem is PartDto p) SelectPart(p);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is PartDto p) SelectPart(p);
            else if (e.Key == Key.Escape) { ClosePopup(); SearchBox.Focus(); }
        }

        private void ClearPart_Click(object sender, RoutedEventArgs e) => ClearSelection();

        // ── Core logic ────────────────────────────────────────────────────────

        private async Task EnsureLoadedAsync()
        {
            if (_allParts.Count > 0) return;
            try
            {
                _allParts = await _partsApi.GetPartsAsync();
                await ApplyExternalSelectionAsync(SelectedPartId);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PartSearch] {ex.Message}"); }
        }

        private async Task ApplyExternalSelectionAsync(int? partId)
        {
            if (!partId.HasValue || partId.Value <= 0)
            {
                return;
            }

            if (_allParts.Count == 0)
            {
                await EnsureLoadedAsync();
            }

            var selected = _allParts.FirstOrDefault(p => p.Id == partId.Value);
            if (selected != null)
            {
                SelectPart(selected);
            }
        }

        private async Task LoadAndFilterAsync()
        {
            await EnsureLoadedAsync();
            ApplyFilter();
            ResultsPopup.IsOpen = true;
            if (FilteredParts.Count > 0)
            {
                _suppressClose = true;
                ResultsList.Focus();
                ResultsList.SelectedIndex = 0;
            }
        }

        private void ApplyFilter()
        {
            var q = (PartSearchText ?? string.Empty).Trim();
            var results = string.IsNullOrEmpty(q)
                ? _allParts
                : _allParts.Where(p =>
                    (p.InternalCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.Barcode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.OEMNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                  ).ToList();

            FilteredParts.Clear();
            foreach (var p in results) FilteredParts.Add(p);

            CountLabel.Text = FilteredParts.Count == 0 ? "no results" : $"{FilteredParts.Count} found";
            EmptyState.Visibility = FilteredParts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ResultsList.Visibility = FilteredParts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (FilteredParts.Count > 0) ResultsPopup.IsOpen = true;
        }

        private PartDto? FindExactScanMatch()
        {
            var q = (PartSearchText ?? string.Empty).Trim();
            if (q.Length == 0)
            {
                return null;
            }

            return _allParts.FirstOrDefault(p =>
                string.Equals(p.Barcode, q, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.InternalCode, q, StringComparison.OrdinalIgnoreCase));
        }

        private void SelectPart(PartDto part)
        {
            SelectedPartId = part.Id;
            SelectedPartPrice = part.SalePrice;
            SelectedPartCostPrice = part.CostPrice;
            SelectedPartDescription = part.Name;           // ← populates Description column

            SelectedNameInline.Text = $"{part.InternalCode} — {part.Name}";
            PriceLabel.Text = $"{part.SalePrice:N0} {part.Currency}";
            PriceLabel.Visibility = Visibility.Visible;

            SelectedIndicator.Visibility = Visibility.Visible;
            SearchIcon.Visibility = Visibility.Collapsed;
            SearchBtn.Visibility = Visibility.Collapsed;
            ClearBtn.Visibility = Visibility.Visible;

            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            PartSearchText = string.Empty;
            SearchBox.Text = string.Empty;
            HidePlaceholder();
            ClosePopup();
        }

        private void ClearSelection()
        {
            SelectedPartId = null;
            SelectedPartPrice = 0;
            SelectedPartCostPrice = 0;
            SelectedPartDescription = string.Empty;

            ClearSelectionVisualState();
        }

        private void ClearSelectionVisualState()
        {
            SelectedPartPrice = 0;
            SelectedPartCostPrice = 0;
            SelectedPartDescription = string.Empty;

            SelectedIndicator.Visibility = Visibility.Collapsed;
            SearchIcon.Visibility = Visibility.Visible;
            SelectedNameInline.Text = string.Empty;
            PriceLabel.Visibility = Visibility.Hidden;
            PriceLabel.Text = string.Empty;
            SearchBtn.Visibility = Visibility.Visible;
            ClearBtn.Visibility = Visibility.Collapsed;

            RestorePillBorder();
            ShowPlaceholder();
            SearchBox.Focus();
        }

        private void ClosePopup() { ResultsPopup.IsOpen = false; _suppressClose = false; }
        private void ShowPlaceholder() => Placeholder.Visibility = Visibility.Visible;
        private void HidePlaceholder() => Placeholder.Visibility = Visibility.Collapsed;
        private void RestorePillBorder()
        {
            InputPill.BorderBrush = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
