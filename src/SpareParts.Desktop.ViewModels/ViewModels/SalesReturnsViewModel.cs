using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class SalesReturnsViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private bool _isBusy;
        private string _status = "Load sales returns.";
        private Brush _statusBrush = Brushes.LightGray;

        private string _searchQuery = string.Empty;
        private DataTable _returnsTable = new();
        private DataRowView? _selectedReturnRow;
        private string _detailText = string.Empty;
        private DataTable _detailItemsTable = new();

        private DataTable _warehousesTable = new();
        private string _invoiceSearch = string.Empty;
        private DataTable _invoiceSearchResultsTable = new();
        private DataRowView? _selectedInvoiceRow;
        private string _selectedInvoiceId = string.Empty;
        private DataTable _returnableLinesTable = new();
        private string _returnDate = DateTime.Today.ToString("yyyy-MM-dd");
        private string _warehouseId = string.Empty;
        private string _reason = string.Empty;
        private string _refundAmount = "0";

        public SalesReturnsViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            ViewDetailCommand = new RelayCommand(_ => ViewDetailAsync().SafeFireAndForget(HandleException));
            LoadWarehousesCommand = new RelayCommand(_ => LoadWarehousesAsync().SafeFireAndForget(HandleException));
            SearchInvoicesCommand = new RelayCommand(_ => SearchInvoicesAsync().SafeFireAndForget(HandleException));
            SelectInvoiceCommand = new RelayCommand(_ => SelectInvoiceAsync().SafeFireAndForget(HandleException));
            SubmitReturnCommand = new RelayCommand(_ => SubmitReturnAsync().SafeFireAndForget(HandleException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand LoadCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand LoadWarehousesCommand { get; }
        public ICommand SearchInvoicesCommand { get; }
        public ICommand SelectInvoiceCommand { get; }
        public ICommand SubmitReturnCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set { _searchQuery = value; OnPropertyChanged(nameof(SearchQuery)); }
        }

        public DataTable ReturnsTable
        {
            get => _returnsTable;
            private set { _returnsTable = value; OnPropertyChanged(nameof(ReturnsTable)); }
        }

        public DataRowView? SelectedReturnRow
        {
            get => _selectedReturnRow;
            set { _selectedReturnRow = value; OnPropertyChanged(nameof(SelectedReturnRow)); }
        }

        public string DetailText
        {
            get => _detailText;
            private set { _detailText = value; OnPropertyChanged(nameof(DetailText)); }
        }

        public DataTable DetailItemsTable
        {
            get => _detailItemsTable;
            private set { _detailItemsTable = value; OnPropertyChanged(nameof(DetailItemsTable)); }
        }

        public DataTable WarehousesTable
        {
            get => _warehousesTable;
            private set { _warehousesTable = value; OnPropertyChanged(nameof(WarehousesTable)); }
        }

        public string InvoiceSearch
        {
            get => _invoiceSearch;
            set { _invoiceSearch = value; OnPropertyChanged(nameof(InvoiceSearch)); }
        }

        public DataTable InvoiceSearchResultsTable
        {
            get => _invoiceSearchResultsTable;
            private set { _invoiceSearchResultsTable = value; OnPropertyChanged(nameof(InvoiceSearchResultsTable)); }
        }

        public DataRowView? SelectedInvoiceRow
        {
            get => _selectedInvoiceRow;
            set { _selectedInvoiceRow = value; OnPropertyChanged(nameof(SelectedInvoiceRow)); }
        }

        public string SelectedInvoiceId
        {
            get => _selectedInvoiceId;
            private set { _selectedInvoiceId = value; OnPropertyChanged(nameof(SelectedInvoiceId)); }
        }

        public DataTable ReturnableLinesTable
        {
            get => _returnableLinesTable;
            private set { _returnableLinesTable = value; OnPropertyChanged(nameof(ReturnableLinesTable)); }
        }

        public string ReturnDate
        {
            get => _returnDate;
            set { _returnDate = value; OnPropertyChanged(nameof(ReturnDate)); }
        }

        public string WarehouseId
        {
            get => _warehouseId;
            set { _warehouseId = value; OnPropertyChanged(nameof(WarehouseId)); }
        }

        public string Reason
        {
            get => _reason;
            set { _reason = value; OnPropertyChanged(nameof(Reason)); }
        }

        public string RefundAmount
        {
            get => _refundAmount;
            set { _refundAmount = value; OnPropertyChanged(nameof(RefundAmount)); }
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            Status = "Loading sales returns...";
            try
            {
                var items = await _crudApi.GetAllAsync<JsonElement>($"/api/sales-returns?search={Uri.EscapeDataString(SearchQuery)}");
                ReturnsTable = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} return(s) loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ViewDetailAsync()
        {
            var id = GetSelectedId(SelectedReturnRow, "returnId") ?? GetSelectedId(SelectedReturnRow, "id");
            if (id == null)
            {
                Status = "Select a return row first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Loading return detail...";
            try
            {
                var detail = await _crudApi.GetAsync<JsonElement>($"/api/sales-returns/{id}");
                var lines = new List<string>();
                AppendLine(lines, detail, "returnNumber", "Return #");
                AppendLine(lines, detail, "originalInvoiceNumber", "Original Invoice");
                AppendLine(lines, detail, "returnDate", "Return Date");
                AppendLine(lines, detail, "totalAmount", "Credit Amount");
                AppendLine(lines, detail, "refundAmount", "Refund Amount");
                AppendLine(lines, detail, "reason", "Reason");
                DetailText = string.Join(Environment.NewLine, lines);

                DetailItemsTable = detail.ValueKind == JsonValueKind.Object &&
                    detail.TryGetProperty("items", out var itemsProp) &&
                    itemsProp.ValueKind == JsonValueKind.Array
                        ? JsonGridHelper.ToDataTable(itemsProp.EnumerateArray().ToList())
                        : new DataTable();

                Status = string.Empty;
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadWarehousesAsync()
        {
            IsBusy = true;
            try
            {
                var items = await _crudApi.GetAllAsync<JsonElement>("/api/warehouses");
                WarehousesTable = JsonGridHelper.ToDataTable(items);
                Status = string.Empty;
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SearchInvoicesAsync()
        {
            if (string.IsNullOrWhiteSpace(InvoiceSearch))
            {
                Status = "Enter an invoice number to search.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Searching invoices...";
            try
            {
                var items = await _crudApi.GetAllAsync<JsonElement>($"/api/sales?search={Uri.EscapeDataString(InvoiceSearch)}");
                InvoiceSearchResultsTable = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} invoice(s) found.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SelectInvoiceAsync()
        {
            var id = GetSelectedId(SelectedInvoiceRow, "invoiceId") ?? GetSelectedId(SelectedInvoiceRow, "id");
            if (id == null)
            {
                Status = "Select an invoice row first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Loading returnable lines...";
            try
            {
                SelectedInvoiceId = id.Value.ToString();
                var lines = await _crudApi.GetAllAsync<JsonElement>($"/api/sales-returns/returnable/{id}");
                var table = JsonGridHelper.ToDataTable(lines);
                table.Columns.Add("returnQuantity", typeof(string));
                foreach (DataRow row in table.Rows)
                {
                    row["returnQuantity"] = "0";
                }
                ReturnableLinesTable = table;
                Status = $"{lines.Count} returnable line(s) loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SubmitReturnAsync()
        {
            var invoiceId = ParseIntOrNull(SelectedInvoiceId);
            var warehouseId = ParseIntOrNull(WarehouseId);
            if (invoiceId == null)
            {
                Status = "Select an invoice and load returnable lines first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            if (warehouseId == null)
            {
                Status = "Select a warehouse.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            var items = new List<object>();
            foreach (DataRow row in ReturnableLinesTable.Rows)
            {
                var quantity = ParseIntOrNull(GetColumnValue(row, "returnQuantity")) ?? 0;
                if (quantity <= 0) continue;

                var partId = ParseIntOrNull(GetColumnValue(row, "partId"));
                if (partId == null) continue;

                var unitPrice = ParseDecimalOrNull(GetColumnValue(row, "unitPrice")) ?? 0;
                var taxRate = ParseDecimalOrNull(GetColumnValue(row, "taxRate")) ?? 0;
                items.Add(new { partId = partId.Value, quantity, unitPrice, taxRate });
            }

            if (items.Count == 0)
            {
                Status = "Select at least one item to return.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Creating return...";
            try
            {
                var returnDate = DateTime.TryParse(ReturnDate, out var parsedDate) ? parsedDate : DateTime.Today;
                var payload = new
                {
                    originalInvoiceId = invoiceId.Value,
                    returnDate = returnDate.ToString("o"),
                    warehouseId = warehouseId.Value,
                    reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason,
                    refundAmount = ParseDecimalOrNull(RefundAmount) ?? 0,
                    items
                };
                await _crudApi.PostAsync("/api/sales-returns", payload);
                Status = "Sales return created.";
                StatusBrush = Brushes.LightGreen;
                Reason = string.Empty;
                RefundAmount = "0";
                ReturnableLinesTable = new DataTable();
                SelectedInvoiceId = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string? GetColumnValue(DataRow row, string columnName)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return row[column]?.ToString();
                }
            }
            return null;
        }

        private static long? GetSelectedId(DataRowView? rowView, string columnName)
        {
            if (rowView == null) return null;
            foreach (DataColumn column in rowView.Row.Table.Columns)
            {
                if (string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return long.TryParse(rowView[column.ColumnName]?.ToString(), out var id) ? id : null;
                }
            }
            return null;
        }

        private static void AppendLine(List<string> lines, JsonElement element, string propertyName, string label)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
            {
                lines.Add($"{label}: {(prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString())}");
            }
        }

        private void HandleException(Exception ex)
        {
            Status = ex.Message;
            StatusBrush = Brushes.OrangeRed;
            IsBusy = false;
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
        private static decimal? ParseDecimalOrNull(string? value) => decimal.TryParse(value, out var result) ? result : null;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
