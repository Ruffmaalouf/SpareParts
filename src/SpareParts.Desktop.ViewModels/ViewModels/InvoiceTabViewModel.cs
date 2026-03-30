using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class InvoiceTabViewModel : INotifyPropertyChanged
    {
        private static int _counter;

        public int TabNumber { get; } = ++_counter;

        private string? _invoiceNumber;
        public string Header => string.IsNullOrWhiteSpace(_invoiceNumber) ? $"Invoice #{TabNumber}" : $"Invoice {_invoiceNumber}";

        public int? InvoiceId { get; private set; }

        public ObservableCollection<PosItemViewModel> Items { get; } = new();
        public ObservableCollection<TransactionTypeDto> TransactionTypes { get; } = new();
        public ObservableCollection<string> CurrencyCodes { get; } = new();

        // ── Invoice header ────────────────────────────────────────────────────

        private int? _customerId;
        public int? CustomerId
        {
            get => _customerId;
            set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); }
        }

        private int? _warehouseId;
        public int? WarehouseId
        {
            get => _warehouseId;
            set { _warehouseId = value; OnPropertyChanged(nameof(WarehouseId)); }
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set { _invoiceDate = value; OnPropertyChanged(nameof(InvoiceDate)); }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                _paidAmount = value;
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            }
        }

        public decimal TotalAmount     => Items.Sum(i => i.LineTotal);
        public decimal RemainingAmount => TotalAmount - PaidAmount;

        // ── New-line entry fields (bound from PartSearchControl + manual) ─────

        private int? _newPartId;
        public int? NewPartId
        {
            get => _newPartId;
            set { _newPartId = value; OnPropertyChanged(nameof(NewPartId)); }
        }

        // Auto-populated when a part is chosen; cashier can still override it
        private decimal _newUnitPrice;
        public decimal NewUnitPrice
        {
            get => _newUnitPrice;
            set { _newUnitPrice = value; OnPropertyChanged(nameof(NewUnitPrice)); }
        }

        private int _newQuantity = 1;
        public int NewQuantity
        {
            get => _newQuantity;
            set { _newQuantity = value; OnPropertyChanged(nameof(NewQuantity)); }
        }

        // Populated automatically when a part is selected — not shown in form, used internally
        private string _newPartDescription = string.Empty;
        public string NewPartDescription
        {
            get => _newPartDescription;
            set { _newPartDescription = value; OnPropertyChanged(nameof(NewPartDescription)); }
        }

        private bool _isLoadedFromSearch;
        public bool IsLoadedFromSearch
        {
            get => _isLoadedFromSearch;
            private set
            {
                if (_isLoadedFromSearch == value)
                {
                    return;
                }

                _isLoadedFromSearch = value;
                OnPropertyChanged(nameof(IsLoadedFromSearch));
                OnPropertyChanged(nameof(IsModifyMode));
                OnPropertyChanged(nameof(SaleModeTitle));
                OnPropertyChanged(nameof(SaleModeSubtitle));
                OnPropertyChanged(nameof(IsInvoiceContentEnabled));
            }
        }

        private bool _isInEditMode;
        public bool IsInEditMode
        {
            get => _isInEditMode;
            private set
            {
                if (_isInEditMode == value)
                {
                    return;
                }

                _isInEditMode = value;
                OnPropertyChanged(nameof(IsInEditMode));
                OnPropertyChanged(nameof(IsEditEnabled));
                OnPropertyChanged(nameof(IsSaveEnabled));
                OnPropertyChanged(nameof(IsResetEnabled));
                OnPropertyChanged(nameof(IsInvoiceContentEnabled));
            }
        }

        public bool IsEditEnabled => IsLoadedFromSearch && !IsInEditMode;
        public bool IsSaveEnabled => IsLoadedFromSearch && IsInEditMode;
        public bool IsResetEnabled => IsLoadedFromSearch && IsInEditMode;
        public bool IsInvoiceContentEnabled => !IsLoadedFromSearch || IsInEditMode;
        public bool IsModifyMode => IsLoadedFromSearch;
        public string SaleModeTitle => IsModifyMode ? "Modify Sale" : "New Sale";
        public string SaleModeSubtitle => IsModifyMode
            ? "Review and modify invoice details below"
            : "Enter invoice details below";

        private int? _selectedTransactionTypeId;
        public int? SelectedTransactionTypeId
        {
            get => _selectedTransactionTypeId;
            set
            {
                if (_selectedTransactionTypeId == value) return;
                _selectedTransactionTypeId = value;
                OnPropertyChanged(nameof(SelectedTransactionTypeId));
                ApplySelectedTransactionType();
            }
        }

        private string _selectedCurrencyCode = "USD";
        public string SelectedCurrencyCode
        {
            get => _selectedCurrencyCode;
            set
            {
                if (string.Equals(_selectedCurrencyCode, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _selectedCurrencyCode = value;
                OnPropertyChanged(nameof(SelectedCurrencyCode));

                if (!_isSynchronizingSelection)
                {
                    ApplySelectedCurrencyCode();
                }
            }
        }

        private decimal _selectedCounterRate = 1m;
        public decimal SelectedCounterRate
        {
            get => _selectedCounterRate;
            private set { _selectedCounterRate = value; OnPropertyChanged(nameof(SelectedCounterRate)); }
        }

        // ── Dependencies / Commands ──────────────────────────────────────────
        private readonly ISalesApiClient _salesApi;
        private readonly ICrudApiClient _crudApi;
        private bool _isSubmitting;
        private InvoiceSnapshot? _snapshot;
        private bool _isSynchronizingSelection;

        public ICommand AddItemCommand      { get; }
        public ICommand SubmitSaleCommand   { get; }
        public ICommand EditInvoiceCommand  { get; }
        public ICommand SaveInvoiceCommand  { get; }
        public ICommand ResetInvoiceCommand { get; }

        public InvoiceTabViewModel(ISalesApiClient salesApi, ICrudApiClient crudApi)
        {
            _salesApi = salesApi;
            _crudApi = crudApi;
            AddItemCommand      = new RelayCommand(_ => AddItem());
            SubmitSaleCommand   = new RelayCommand(_ => _ = SubmitSaleAsync());
            EditInvoiceCommand  = new RelayCommand(_ => BeginEditMode());
            SaveInvoiceCommand  = new RelayCommand(_ => _ = SaveEditsAsync());
            ResetInvoiceCommand = new RelayCommand(_ => ResetEdits());

            Items.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            };

            _ = LoadTransactionTypesAsync();
        }

        public void SelectTransactionTypeByName(string transactionTypeName)
        {
            var selected = TransactionTypes.FirstOrDefault(t =>
                string.Equals(t.Name, transactionTypeName, StringComparison.OrdinalIgnoreCase));

            if (selected != null)
            {
                SelectedTransactionTypeId = selected.Id;
            }
        }

        public void MarkOpenedFromSearch()
        {
            IsLoadedFromSearch = true;
            IsInEditMode = false;
            OnPropertyChanged(nameof(IsEditEnabled));
            OnPropertyChanged(nameof(IsSaveEnabled));
            OnPropertyChanged(nameof(IsResetEnabled));

            if (_snapshot == null)
            {
                _snapshot = CreateSnapshot();
            }
        }

        public void LoadFromDatabase(SalesInvoiceDetailsDto invoice)
        {
            InvoiceId = invoice.InvoiceId;
            _invoiceNumber = invoice.InvoiceNumber;
            OnPropertyChanged(nameof(Header));

            CustomerId = invoice.CustomerId;
            WarehouseId = invoice.WarehouseId;
            InvoiceDate = invoice.InvoiceDate;
            PaidAmount = invoice.PaidAmount;

            Items.Clear();
            foreach (var line in invoice.Items)
            {
                Items.Add(new PosItemViewModel
                {
                    PartId = line.PartId,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }

            MarkOpenedFromSearch();
        }

        // ── Add line ──────────────────────────────────────────────────────────

        private void AddItem()
        {
            if (NewPartId == null || NewPartId <= 0)
            {
                CustomMessageBox.Show("Please select a part first.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Please select a part first.", false);
                return;
            }
            if (NewQuantity <= 0)
            {
                CustomMessageBox.Show("Quantity must be greater than zero.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Quantity must be greater than zero.", false);
                return;
            }
            if (NewUnitPrice <= 0)
            {
                CustomMessageBox.Show("Unit price must be greater than zero.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Unit price must be greater than zero.", false);
                return;
            }

            Items.Add(new PosItemViewModel
            {
                PartId      = NewPartId.Value,
                Description = NewPartDescription,
                Quantity    = NewQuantity,
                UnitPrice   = NewUnitPrice
            });

            NewPartId          = null;
            NewQuantity        = 1;
            NewUnitPrice       = 0;
            NewPartDescription = string.Empty;

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(RemainingAmount));
        }

        private void BeginEditMode()
        {
            if (!IsLoadedFromSearch)
            {
                return;
            }

            _snapshot ??= CreateSnapshot();
            IsInEditMode = true;
            AppNotificationCenter.Instance.Publish($"✎ {Header} is now editable.", true);
        }

        private async Task SaveEditsAsync()
        {
            if (!IsLoadedFromSearch)
            {
                return;
            }

            if (!IsInEditMode)
            {
                return;
            }

            if (InvoiceId == null || InvoiceId <= 0)
            {
                AppNotificationCenter.Instance.Publish("✗ Cannot save invoice: missing invoice id.", false);
                return;
            }

            if (WarehouseId == null || WarehouseId <= 0)
            {
                CustomMessageBox.Show("Please select a warehouse.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Please select a warehouse.", false);
                return;
            }

            if (Items.Count == 0)
            {
                CustomMessageBox.Show("Add at least one line before saving.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Add at least one line before saving.", false);
                return;
            }

            try
            {
                var request = new UpdateSaleRequest
                {
                    InvoiceDate = InvoiceDate,
                    CustomerId = CustomerId,
                    WarehouseId = WarehouseId.Value,
                    PaidAmount = PaidAmount,
                    Items = Items.Select(i => new SaleItemDto
                    {
                        PartId = i.PartId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        DiscountAmount = 0,
                        TaxRate = 0
                    }).ToList()
                };

                await _salesApi.UpdateSaleAsync(InvoiceId.Value, request);
            }
            catch (ApiClientException ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", "Error");
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
                return;
            }
            catch (Exception)
            {
                CustomMessageBox.Show("Unexpected error while saving invoice.", "Error", "Error");
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while saving invoice.", false);
                return;
            }

            _snapshot = CreateSnapshot();
            IsInEditMode = false;
            AppNotificationCenter.Instance.Publish($"✓ Changes saved to database for {Header}.", true);
        }

        private async Task LoadTransactionTypesAsync()
        {
            try
            {
                var rows = await _crudApi.GetAllAsync<TransactionTypeDto>("api/transactiontypes");
                TransactionTypes.Clear();
                foreach (var row in rows.Where(t => t.IsActive))
                {
                    TransactionTypes.Add(row);
                }

                CurrencyCodes.Clear();
                foreach (var code in TransactionTypes
                    .Select(t => (t.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant())
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    CurrencyCodes.Add(code);
                }

                if (SelectedTransactionTypeId == null && TransactionTypes.Count > 0)
                {
                    var salesType = TransactionTypes.FirstOrDefault(t => string.Equals(t.Name, "Sales", StringComparison.OrdinalIgnoreCase));
                    SelectedTransactionTypeId = salesType?.Id ?? TransactionTypes[0].Id;
                }
            }
            catch
            {
                // Non-blocking for invoice operations.
            }
        }

        private void ApplySelectedTransactionType()
        {
            var selected = TransactionTypes.FirstOrDefault(t => t.Id == SelectedTransactionTypeId);
            _isSynchronizingSelection = true;
            if (selected == null)
            {
                _selectedCurrencyCode = "USD";
                OnPropertyChanged(nameof(SelectedCurrencyCode));
                SelectedCounterRate = 1m;
                _isSynchronizingSelection = false;
                return;
            }

            _selectedCurrencyCode = selected.CurrencyCode;
            OnPropertyChanged(nameof(SelectedCurrencyCode));
            SelectedCounterRate = selected.CounterRate;
            _isSynchronizingSelection = false;
        }

        private void ApplySelectedCurrencyCode()
        {
            var selected = TransactionTypes.FirstOrDefault(t =>
                string.Equals(t.CurrencyCode, SelectedCurrencyCode, StringComparison.OrdinalIgnoreCase));

            if (selected == null)
            {
                SelectedCounterRate = 1m;
                return;
            }

            if (SelectedTransactionTypeId != selected.Id)
            {
                SelectedTransactionTypeId = selected.Id;
                return;
            }

            SelectedCounterRate = selected.CounterRate;
        }

        private void ResetEdits()
        {
            if (!IsLoadedFromSearch || _snapshot == null || !IsInEditMode)
            {
                return;
            }

            CustomerId = _snapshot.CustomerId;
            WarehouseId = _snapshot.WarehouseId;
            PaidAmount = _snapshot.PaidAmount;
            InvoiceDate = _snapshot.InvoiceDate;

            Items.Clear();
            foreach (var line in _snapshot.Items)
            {
                Items.Add(new PosItemViewModel
                {
                    PartId = line.PartId,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }

            IsInEditMode = false;
            AppNotificationCenter.Instance.Publish($"↺ {Header} restored to last saved values.", true);
        }

        private InvoiceSnapshot CreateSnapshot() => new()
        {
            CustomerId = CustomerId,
            WarehouseId = WarehouseId,
            PaidAmount = PaidAmount,
            InvoiceDate = InvoiceDate,
            Items = Items.Select(i => new InvoiceSnapshotLine
            {
                PartId = i.PartId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        // ── Submit ────────────────────────────────────────────────────────────

        private async Task SubmitSaleAsync()
        {
            if (_isSubmitting)
            {
                return;
            }

            if (Items.Count == 0)
            {
                CustomMessageBox.Show("Add at least one line before submitting.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Add at least one line before submitting.", false);
                return;
            }
            if (WarehouseId == null)
            {
                CustomMessageBox.Show("Please select a warehouse.", "Validation", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Please select a warehouse.", false);
                return;
            }

            try
            {
                _isSubmitting = true;
                var req = new CreateSaleRequest
                {
                    InvoiceDate   = InvoiceDate,
                    CustomerId    = CustomerId,
                    WarehouseId   = WarehouseId.Value,
                    PaymentMethod = "Cash",
                    PaidAmount    = PaidAmount,
                    Items         = Items.Select(i => new SaleItemDto
                    {
                        PartId         = i.PartId,
                        Quantity       = i.Quantity,
                        UnitPrice      = i.UnitPrice,
                        DiscountAmount = 0,
                        TaxRate        = 0
                    }).ToList()
                };

                var result = await _salesApi.CreateSaleAsync(req);

                CustomMessageBox.Show(
                    $"Invoice {result.InvoiceNumber} created.\nTotal: {result.TotalAmount:N0}",
                    "Sale Submitted", "Success");
                AppNotificationCenter.Instance.Publish($"✓ Invoice {result.InvoiceNumber} created.", true);

                Items.Clear();
                PaidAmount = 0;
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            }
            catch (ApiClientException ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", "Error");
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                CustomMessageBox.Show("Unexpected error while submitting sale.", "Error", "Error");
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while submitting sale.", false);
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    }
}
