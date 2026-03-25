using SpareParts.Domain.BusinessPartners;
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
    public partial class CustomerSearchControl : UserControl
    {

        private readonly ICrudApiClient _crudApi;
        private List<CustomerDto> _allCustomers = new();
        private bool _suppressClose;

        // ── Dependency Properties ─────────────────────────────────────────────

        public static readonly DependencyProperty SelectedCustomerIdProperty =
            DependencyProperty.Register(
                nameof(SelectedCustomerId), typeof(int?), typeof(CustomerSearchControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedCustomerIdChanged));

        public int? SelectedCustomerId
        {
            get => (int?)GetValue(SelectedCustomerIdProperty);
            set => SetValue(SelectedCustomerIdProperty, value);
        }

        private static void OnSelectedCustomerIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CustomerSearchControl control)
            {
                return;
            }

            _ = control.ApplyExternalSelectionAsync(e.NewValue as int?);
        }

        public static readonly DependencyProperty CustomerSearchTextProperty =
            DependencyProperty.Register(nameof(CustomerSearchText), typeof(string),
                typeof(CustomerSearchControl), new PropertyMetadata(string.Empty));

        public string CustomerSearchText
        {
            get => (string)GetValue(CustomerSearchTextProperty);
            set => SetValue(CustomerSearchTextProperty, value);
        }

        public static readonly DependencyProperty FilteredCustomersProperty =
            DependencyProperty.Register(nameof(FilteredCustomers),
                typeof(ObservableCollection<CustomerDto>), typeof(CustomerSearchControl),
                new PropertyMetadata(new ObservableCollection<CustomerDto>()));

        public ObservableCollection<CustomerDto> FilteredCustomers
        {
            get => (ObservableCollection<CustomerDto>)GetValue(FilteredCustomersProperty);
            set => SetValue(FilteredCustomersProperty, value);
        }

        public static readonly DependencyProperty HasSelectedCustomerProperty =
            DependencyProperty.Register(nameof(HasSelectedCustomer), typeof(bool),
                typeof(CustomerSearchControl), new PropertyMetadata(false));

        public bool HasSelectedCustomer
        {
            get => (bool)GetValue(HasSelectedCustomerProperty);
            set => SetValue(HasSelectedCustomerProperty, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public CustomerSearchControl() : this(new CrudApiClient())
        {
        }

        public CustomerSearchControl(ICrudApiClient crudApi)
        {
            InitializeComponent();
            _crudApi = crudApi;
            FilteredCustomers = new ObservableCollection<CustomerDto>();
            Loaded += async (_, _) => await EnsureLoadedAsync();

            // Global mouse-down handler to close popup on outside click
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
            _ = LoadAndFilterAsync();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (FilteredCustomers.Count == 1)
                    { SelectCustomer(FilteredCustomers[0]); return; }
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
                    if (_allCustomers.Count > 0) ApplyFilter();
                    break;
            }
        }

        private void ResultItem_Click(object sender, MouseButtonEventArgs e)
        {
            _suppressClose = true;
            if (ResultsList.SelectedItem is CustomerDto c) SelectCustomer(c);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is CustomerDto c)
                SelectCustomer(c);
            else if (e.Key == Key.Escape)
            {
                ClosePopup();
                SearchBox.Focus();
            }
        }

        private void ClearCustomer_Click(object sender, RoutedEventArgs e)
            => ClearSelection();

        // ── Core logic ────────────────────────────────────────────────────────

        private async Task EnsureLoadedAsync()
        {
            if (_allCustomers.Count > 0) return;
            try
            {
                _allCustomers = await _crudApi.GetAllAsync<CustomerDto>("api/customers");
                await ApplyExternalSelectionAsync(SelectedCustomerId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerSearch] pre-load: {ex.Message}");
            }
        }

        private async Task ApplyExternalSelectionAsync(int? customerId)
        {
            if (!customerId.HasValue || customerId.Value <= 0)
            {
                HasSelectedCustomer = false;
                SelectedIndicator.Visibility = Visibility.Collapsed;
                SearchIcon.Visibility = Visibility.Visible;
                SelectedNameInline.Text = string.Empty;
                PhoneLabel.Visibility = Visibility.Collapsed;
                PhoneLabel.Text = string.Empty;
                SearchBtn.Visibility = Visibility.Visible;
                ClearBtn.Visibility = Visibility.Collapsed;
                return;
            }

            if (_allCustomers.Count == 0)
            {
                await EnsureLoadedAsync();
            }

            var selected = _allCustomers.FirstOrDefault(c => c.Id == customerId.Value);
            if (selected != null)
            {
                SelectCustomer(selected);
            }
        }

        private async Task LoadAndFilterAsync()
        {
            await EnsureLoadedAsync();
            ApplyFilter();
            ResultsPopup.IsOpen = true;

            if (FilteredCustomers.Count > 0)
            {
                _suppressClose = true;
                ResultsList.Focus();
                ResultsList.SelectedIndex = 0;
            }
        }

        private void ApplyFilter()
        {
            var q = (CustomerSearchText ?? string.Empty).Trim();

            var results = string.IsNullOrEmpty(q)
                ? _allCustomers
                : _allCustomers.Where(c =>
                    (c.Name  ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (c.Phone ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                  ).ToList();

            FilteredCustomers.Clear();
            foreach (var c in results) FilteredCustomers.Add(c);

            CountLabel.Text = FilteredCustomers.Count == 0
                ? "no results"
                : $"{FilteredCustomers.Count} found";

            EmptyState.Visibility  = FilteredCustomers.Count == 0 ? Visibility.Visible  : Visibility.Collapsed;
            ResultsList.Visibility = FilteredCustomers.Count > 0  ? Visibility.Visible  : Visibility.Collapsed;

            if (FilteredCustomers.Count > 0)
                ResultsPopup.IsOpen = true;
        }

        private void SelectCustomer(CustomerDto customer)
        {
            SelectedCustomerId   = customer.Id;
            HasSelectedCustomer  = true;

            SelectedNameInline.Text = customer.Name;
            PhoneLabel.Text         = customer.Phone ?? string.Empty;
            PhoneLabel.Visibility   = string.IsNullOrEmpty(customer.Phone)
                                      ? Visibility.Collapsed : Visibility.Visible;

            SelectedIndicator.Visibility = Visibility.Visible;
            SearchIcon.Visibility        = Visibility.Collapsed;
            SearchBtn.Visibility         = Visibility.Collapsed;
            ClearBtn.Visibility          = Visibility.Visible;

            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);

            CustomerSearchText  = string.Empty;
            SearchBox.Text      = string.Empty;
            HidePlaceholder();
            ClosePopup();
        }

        private void ClearSelection()
        {
            SelectedCustomerId   = null;
            HasSelectedCustomer  = false;

            SelectedIndicator.Visibility = Visibility.Collapsed;
            SearchIcon.Visibility        = Visibility.Visible;
            SelectedNameInline.Text      = string.Empty;
            PhoneLabel.Visibility        = Visibility.Collapsed;
            PhoneLabel.Text              = string.Empty;

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
