using SpareParts.Domain.BusinessPartners;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public partial class CustomerSearchControl : UserControl
    {
        // ── HTTP ──────────────────────────────────────────────────────────────
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private List<CustomerDto> _allCustomers = new();

        // ═════════════════════════════════════════════════════════════════════
        // Dependency Properties
        // ═════════════════════════════════════════════════════════════════════

        public static readonly DependencyProperty SelectedCustomerIdProperty =
            DependencyProperty.Register(
                nameof(SelectedCustomerId),
                typeof(int?),
                typeof(CustomerSearchControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int? SelectedCustomerId
        {
            get => (int?)GetValue(SelectedCustomerIdProperty);
            set => SetValue(SelectedCustomerIdProperty, value);
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

        // ═════════════════════════════════════════════════════════════════════
        // Constructor
        // ═════════════════════════════════════════════════════════════════════

        public CustomerSearchControl()
        {
            InitializeComponent();
            FilteredCustomers = new ObservableCollection<CustomerDto>();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Event Handlers
        // ═════════════════════════════════════════════════════════════════════

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAndFilter();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            // Highlight the input pill border with accent colour
            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);

            // Auto-search if box gets focus with text already in it
            if (!string.IsNullOrEmpty(SearchBox.Text) && _allCustomers.Count > 0)
                ApplyFilter();
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (FilteredCustomers.Count == 1)
                {
                    SelectCustomer(FilteredCustomers[0]);
                    return;
                }
                LoadAndFilter();
            }
            else if (e.Key == Key.Escape)
            {
                ResultsPopup.IsOpen = false;
                RestorePillBorder();
            }
            else if (e.Key == Key.Down && ResultsPopup.IsOpen)
            {
                ResultsList.Focus();
                if (ResultsList.Items.Count > 0)
                    ResultsList.SelectedIndex = 0;
            }
            else
            {
                // Show/hide placeholder
                if (string.IsNullOrEmpty(SearchBox.Text))
                    ShowPlaceholder();
                else
                    HidePlaceholder();

                // Live filter if already loaded
                if (_allCustomers.Count > 0)
                    ApplyFilter();
            }
        }

        private void ResultItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedItem is CustomerDto c)
                SelectCustomer(c);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is CustomerDto c)
                SelectCustomer(c);
            else if (e.Key == Key.Escape)
            {
                ResultsPopup.IsOpen = false;
                SearchBox.Focus();
            }
        }

        private void ClearCustomer_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Core Logic
        // ═════════════════════════════════════════════════════════════════════

        private void LoadAndFilter()
        {
            try
            {
                var json = _http.GetStringAsync("api/customers").Result;
                _allCustomers = JsonSerializer.Deserialize<List<CustomerDto>>(json, _json)
                                ?? new List<CustomerDto>();
            }
            catch
            {
                _allCustomers = new List<CustomerDto>();
            }

            ApplyFilter();
            ResultsPopup.IsOpen = true;

            if (FilteredCustomers.Count > 0)
            {
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
            foreach (var c in results)
                FilteredCustomers.Add(c);

            // Update count label in popup header
            CountLabel.Text = FilteredCustomers.Count == 0
                ? "no results"
                : $"{FilteredCustomers.Count} found";

            // Show/hide empty state vs list
            EmptyState.Visibility  = FilteredCustomers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ResultsList.Visibility = FilteredCustomers.Count > 0  ? Visibility.Visible : Visibility.Collapsed;

            if (FilteredCustomers.Count > 0)
                ResultsPopup.IsOpen = true;
        }

        private void SelectCustomer(CustomerDto customer)
        {
            SelectedCustomerId = customer.Id;

            // Update inline display
            SelectedNameInline.Text = customer.Name;
            PhoneLabel.Text         = customer.Phone ?? string.Empty;
            PhoneLabel.Visibility   = string.IsNullOrEmpty(customer.Phone)
                                      ? Visibility.Collapsed : Visibility.Visible;

            // Switch pill to "selected" state
            SelectedIndicator.Visibility = Visibility.Visible;
            SearchBtn.Visibility         = Visibility.Collapsed;
            ClearBtn.Visibility          = Visibility.Visible;

            // Accent border
            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);

            // Clear search text + placeholder
            CustomerSearchText  = string.Empty;
            SearchBox.Text      = string.Empty;
            ShowPlaceholder();

            ResultsPopup.IsOpen = false;
        }

        private void ClearSelection()
        {
            SelectedCustomerId = null;

            SelectedIndicator.Visibility = Visibility.Collapsed;
            SelectedNameInline.Text      = string.Empty;
            PhoneLabel.Visibility        = Visibility.Collapsed;
            PhoneLabel.Text              = string.Empty;

            SearchBtn.Visibility = Visibility.Visible;
            ClearBtn.Visibility  = Visibility.Collapsed;

            RestorePillBorder();
            ShowPlaceholder();
            SearchBox.Focus();
        }

        // ─── Visual helpers ───────────────────────────────────────────────────

        private void ShowPlaceholder() => Placeholder.Visibility = Visibility.Visible;
        private void HidePlaceholder() => Placeholder.Visibility = Visibility.Collapsed;

        private void RestorePillBorder()
        {
            InputPill.BorderBrush     = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
