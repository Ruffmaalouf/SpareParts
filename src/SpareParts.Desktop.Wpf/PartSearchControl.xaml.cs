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
        private List<PartDto> _allParts = new();
        private bool _suppressClose;   // prevents popup closing on internal clicks

        // ── Dependency Properties ─────────────────────────────────────────────

        public static readonly DependencyProperty SelectedPartIdProperty =
            DependencyProperty.Register(
                nameof(SelectedPartId), typeof(int?), typeof(PartSearchControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int? SelectedPartId
        {
            get => (int?)GetValue(SelectedPartIdProperty);
            set => SetValue(SelectedPartIdProperty, value);
        }

        public static readonly DependencyProperty SelectedPartPriceProperty =
            DependencyProperty.Register(
                nameof(SelectedPartPrice), typeof(decimal), typeof(PartSearchControl),
                new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public decimal SelectedPartPrice
        {
            get => (decimal)GetValue(SelectedPartPriceProperty);
            set => SetValue(SelectedPartPriceProperty, value);
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
                new PropertyMetadata(new ObservableCollection<PartDto>()));

        public ObservableCollection<PartDto> FilteredParts
        {
            get => (ObservableCollection<PartDto>)GetValue(FilteredPartsProperty);
            set => SetValue(FilteredPartsProperty, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public PartSearchControl()
        {
            InitializeComponent();
            FilteredParts = new ObservableCollection<PartDto>();
            Loaded += async (_, _) => await EnsureLoadedAsync();

            // Close popup when user clicks outside this control
            Application.Current.MainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ResultsPopup.IsOpen) return;
            if (_suppressClose) { _suppressClose = false; return; }

            // Check if click is inside our popup or input pill
            var hit = e.OriginalSource as DependencyObject;
            if (IsDescendantOfPopupOrPill(hit)) return;

            ClosePopup();
        }

        private bool IsDescendantOfPopupOrPill(DependencyObject? d)
        {
            while (d != null)
            {
                if (d == ResultsPopup || d == InputPill) return true;
                // Also check the popup's child (the Border)
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
            _ = LoadAndFilterAsync();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);

            if (_allParts.Count > 0)
                ApplyFilter();
            else
                _ = LoadAndFilterAsync();
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Only close if focus moved outside the control entirely
            // (not to the ResultsList inside the popup)
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (IsDescendantOfPopupOrPill(focused)) return;
            // Don't close here — let MainWindow_PreviewMouseDown handle it
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (FilteredParts.Count == 1)
                    { SelectPart(FilteredParts[0]); return; }
                    _ = LoadAndFilterAsync();
                    break;

                case Key.Escape:
                    ClosePopup();
                    RestorePillBorder();
                    break;

                case Key.Down when ResultsPopup.IsOpen:
                    _suppressClose = true;
                    ResultsList.Focus();
                    if (ResultsList.Items.Count > 0)
                        ResultsList.SelectedIndex = 0;
                    break;

                default:
                    HidePlaceholder();
                    if (string.IsNullOrEmpty(SearchBox.Text)) ShowPlaceholder();
                    if (_allParts.Count > 0) ApplyFilter();
                    break;
            }
        }

        private void ResultItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            _suppressClose = true;
            if (ResultsList.SelectedItem is PartDto p) SelectPart(p);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is PartDto p)
                SelectPart(p);
            else if (e.Key == Key.Escape)
            {
                ClosePopup();
                SearchBox.Focus();
            }
        }

        private void ClearPart_Click(object sender, RoutedEventArgs e)
            => ClearSelection();

        private void Popup_MouseLeave(object sender, MouseEventArgs e)
        {
            // Do NOT close on mouse-leave — only close on outside click
        }

        // ── Core logic ────────────────────────────────────────────────────────

        private async Task EnsureLoadedAsync()
        {
            if (_allParts.Count > 0) return;
            try
            {
                _allParts = await ApiClient.Instance.GetPartsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartSearch] pre-load: {ex.Message}");
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
                    (p.Name         ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.OEMNumber    ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                  ).ToList();

            FilteredParts.Clear();
            foreach (var p in results) FilteredParts.Add(p);

            CountLabel.Text = FilteredParts.Count == 0
                ? "no results"
                : $"{FilteredParts.Count} found";

            EmptyState.Visibility  = FilteredParts.Count == 0 ? Visibility.Visible  : Visibility.Collapsed;
            ResultsList.Visibility = FilteredParts.Count > 0  ? Visibility.Visible  : Visibility.Collapsed;

            if (FilteredParts.Count > 0)
                ResultsPopup.IsOpen = true;
        }

        private void SelectPart(PartDto part)
        {
            SelectedPartId    = part.Id;
            SelectedPartPrice = part.SalePrice;

            SelectedNameInline.Text = $"{part.InternalCode} — {part.Name}";
            PriceLabel.Text         = $"{part.SalePrice:N0} {part.Currency}";
            PriceLabel.Visibility   = Visibility.Visible;

            SelectedIndicator.Visibility = Visibility.Visible;
            SearchIcon.Visibility        = Visibility.Collapsed;
            SearchBtn.Visibility         = Visibility.Collapsed;
            ClearBtn.Visibility          = Visibility.Visible;

            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);

            PartSearchText  = string.Empty;
            SearchBox.Text  = string.Empty;
            ShowPlaceholder();
            ClosePopup();
        }

        private void ClearSelection()
        {
            SelectedPartId    = null;
            SelectedPartPrice = 0;

            SelectedIndicator.Visibility = Visibility.Collapsed;
            SearchIcon.Visibility        = Visibility.Visible;
            SelectedNameInline.Text      = string.Empty;
            PriceLabel.Visibility        = Visibility.Collapsed;
            PriceLabel.Text              = string.Empty;

            SearchBtn.Visibility = Visibility.Visible;
            ClearBtn.Visibility  = Visibility.Collapsed;

            RestorePillBorder();
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

        private void RestorePillBorder()
        {
            InputPill.BorderBrush     = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
