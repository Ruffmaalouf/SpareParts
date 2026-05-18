using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Transactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartPurchasesViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private readonly IPurchasesApiClient _purchasesApi;
        private string _searchText = string.Empty;
        private PurchaseInvoiceLookupDto? _selectedPurchase;
        private PurchaseEditorItemViewModel? _selectedItem;
        private int? _purchaseId;
        private string _purchaseNumber = string.Empty;
        private int? _supplierId;
        private int? _warehouseId;
        private DateTime _purchaseDate = DateTime.Today;
        private decimal _paidAmount;
        private bool _isLoading;
        private bool _isInEditMode;
        private string _status = "Part purchases are ready.";
        private Brush _statusBrush = Brushes.LightGreen;
        private int? _newPartId;
        private string _newPartDescription = string.Empty;
        private decimal _newUnitCost;
        private int _newQuantity = 1;
        private PurchaseEditorSnapshot? _snapshot;
        private int _selectedPurchaseVersion;

        public ObservableCollection<SupplierDto> Suppliers { get; } = new();
        public ObservableCollection<PurchaseInvoiceLookupDto> RecentPurchases { get; } = new();
        public ObservableCollection<PurchaseEditorItemViewModel> Items { get; } = new();
        public ObservableCollection<TransactionTimelineStepDto> PostingTimeline { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public PurchaseInvoiceLookupDto? SelectedPurchase
        {
            get => _selectedPurchase;
            set
            {
                if (ReferenceEquals(_selectedPurchase, value)) return;
                _selectedPurchase = value;
                OnPropertyChanged(nameof(SelectedPurchase));
                LoadSelectedPurchaseAsync(value?.PurchaseId).SafeFireAndForget(HandleBackgroundException);
            }
        }

        public PurchaseEditorItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (ReferenceEquals(_selectedItem, value)) return;
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                OnPropertyChanged(nameof(CanRemoveItem));
            }
        }

        public int? PurchaseId
        {
            get => _purchaseId;
            private set
            {
                if (_purchaseId == value) return;
                _purchaseId = value;
                OnPropertyChanged(nameof(PurchaseId));
                RaiseModeProperties();
            }
        }

        public string PurchaseNumber
        {
            get => _purchaseNumber;
            private set
            {
                if (_purchaseNumber == value) return;
                _purchaseNumber = value;
                OnPropertyChanged(nameof(PurchaseNumber));
                RaiseModeProperties();
            }
        }

        public int? SupplierId
        {
            get => _supplierId;
            set
            {
                if (_supplierId == value) return;
                _supplierId = value;
                OnPropertyChanged(nameof(SupplierId));
            }
        }

        public int? WarehouseId
        {
            get => _warehouseId;
            set
            {
                if (_warehouseId == value) return;
                _warehouseId = value;
                OnPropertyChanged(nameof(WarehouseId));
            }
        }

        public DateTime PurchaseDate
        {
            get => _purchaseDate;
            set
            {
                if (_purchaseDate == value) return;
                _purchaseDate = value;
                OnPropertyChanged(nameof(PurchaseDate));
            }
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (_paidAmount == value) return;
                _paidAmount = value;
                OnPropertyChanged(nameof(PaidAmount));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public bool IsInEditMode
        {
            get => _isInEditMode;
            private set
            {
                if (_isInEditMode == value) return;
                _isInEditMode = value;
                OnPropertyChanged(nameof(IsInEditMode));
                RaiseModeProperties();
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value) return;
                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public int? NewPartId
        {
            get => _newPartId;
            set
            {
                if (_newPartId == value) return;
                _newPartId = value;
                OnPropertyChanged(nameof(NewPartId));
            }
        }

        public string NewPartDescription
        {
            get => _newPartDescription;
            set
            {
                if (_newPartDescription == value) return;
                _newPartDescription = value;
                OnPropertyChanged(nameof(NewPartDescription));
            }
        }

        public decimal NewUnitCost
        {
            get => _newUnitCost;
            set
            {
                if (_newUnitCost == value) return;
                _newUnitCost = value;
                OnPropertyChanged(nameof(NewUnitCost));
            }
        }

        public int NewQuantity
        {
            get => _newQuantity;
            set
            {
                if (_newQuantity == value) return;
                _newQuantity = value;
                OnPropertyChanged(nameof(NewQuantity));
            }
        }

        public decimal TotalAmount => decimal.Round(Items.Sum(item => item.LineTotal), 4, MidpointRounding.AwayFromZero);
        public bool IsLoadedPurchase => PurchaseId is > 0;
        public bool IsEditorEnabled => !IsLoadedPurchase || IsInEditMode;
        public bool IsItemsGridReadOnly => !IsEditorEnabled;
        public bool IsEditEnabled => IsLoadedPurchase && !IsInEditMode;
        public bool CanRemoveItem => IsEditorEnabled && SelectedItem != null;
        public bool HasPostingTimeline => PostingTimeline.Count > 0;
        public string EditorTitle => IsLoadedPurchase
            ? $"Purchase {PurchaseNumber}"
            : "New Purchase";
        public string EditorSubtitle => IsLoadedPurchase
            ? (IsInEditMode
                ? "Update the saved purchase and save your changes."
                : "Open a saved purchase, then switch to edit mode to modify it.")
            : "Create a supplier purchase that posts into inventory and accounting like sales do.";
        public string SearchSummary => $"{RecentPurchases.Count} purchase(s) loaded.";

        public ICommand LoadCommand { get; }
        public ICommand RefreshSearchCommand { get; }
        public ICommand NewPurchaseCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SubmitCommand { get; }

        public PartPurchasesViewModel(ICrudApiClient crudApi, IPurchasesApiClient purchasesApi)
        {
            _crudApi = crudApi;
            _purchasesApi = purchasesApi;

            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshSearchCommand = new RelayCommand(_ => RefreshSearchResultsAsync().SafeFireAndForget(HandleBackgroundException));
            NewPurchaseCommand = new RelayCommand(_ => StartNewPurchase());
            EditCommand = new RelayCommand(_ => BeginEdit());
            ResetCommand = new RelayCommand(_ => ResetEditor());
            AddItemCommand = new RelayCommand(_ => AddItem());
            RemoveItemCommand = new RelayCommand(_ => RemoveSelectedItem());
            SubmitCommand = new RelayCommand(_ => SubmitAsync().SafeFireAndForget(HandleBackgroundException));

            Items.CollectionChanged += Items_CollectionChanged;
        }

        public async Task LoadAsync(int? purchaseIdToSelect = null)
        {
            IsLoading = true;
            SetStatus("Loading part purchases…", true);

            try
            {
                var suppliersTask = _crudApi.GetAllAsync<SupplierDto>("api/suppliers");
                var purchasesTask = _purchasesApi.SearchPurchasesAsync(SearchText ?? string.Empty);

                await Task.WhenAll(suppliersTask, purchasesTask);

                Replace(Suppliers, suppliersTask.Result.OrderBy(item => item.Name));
                Replace(RecentPurchases, purchasesTask.Result);
                OnPropertyChanged(nameof(SearchSummary));

                var targetPurchaseId = purchaseIdToSelect ?? PurchaseId ?? SelectedPurchase?.PurchaseId;
                if (targetPurchaseId is > 0)
                {
                    SelectedPurchase = RecentPurchases.FirstOrDefault(item => item.PurchaseId == targetPurchaseId);
                }
                else if (!IsLoadedPurchase && SupplierId is not > 0)
                {
                    SupplierId = Suppliers.FirstOrDefault()?.Id;
                }

                SetStatus("Part purchase workspace loaded.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Loading part purchases failed: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshSearchResultsAsync()
        {
            try
            {
                var purchases = await _purchasesApi.SearchPurchasesAsync(SearchText ?? string.Empty);
                Replace(RecentPurchases, purchases);
                OnPropertyChanged(nameof(SearchSummary));

                if (PurchaseId is > 0)
                {
                    var stillSelected = RecentPurchases.FirstOrDefault(item => item.PurchaseId == PurchaseId.Value);
                    if (!ReferenceEquals(SelectedPurchase, stillSelected))
                    {
                        SelectedPurchase = stillSelected;
                    }
                }

                SetStatus("Purchase search refreshed.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Refreshing purchases failed: {ex.Message}", false);
            }
        }

        private async Task LoadSelectedPurchaseAsync(int? purchaseId)
        {
            var version = ++_selectedPurchaseVersion;
            if (purchaseId is not > 0)
            {
                return;
            }

            try
            {
                var purchase = await _purchasesApi.GetPurchaseAsync(purchaseId.Value);
                if (version != _selectedPurchaseVersion || purchase == null)
                {
                    return;
                }

                LoadFromPurchase(purchase);
                SetStatus($"Purchase {purchase.PurchaseNumber} loaded.", true);
            }
            catch (Exception ex)
            {
                if (version != _selectedPurchaseVersion)
                {
                    return;
                }

                SetStatus($"Loading purchase failed: {ex.Message}", false);
            }
        }

        private void LoadFromPurchase(PurchaseInvoiceDetailsDto purchase)
        {
            PurchaseId = purchase.PurchaseId;
            PurchaseNumber = purchase.PurchaseNumber;
            SupplierId = purchase.SupplierId;
            WarehouseId = purchase.WarehouseId;
            PurchaseDate = purchase.PurchaseDate;
            PaidAmount = purchase.PaidAmount;

            Replace(Items, purchase.Items.Select(item => new PurchaseEditorItemViewModel
            {
                PartId = item.PartId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            }));
            Replace(PostingTimeline, purchase.Timeline);
            OnPropertyChanged(nameof(HasPostingTimeline));

            IsInEditMode = false;
            _snapshot = CreateSnapshot();
            OnPropertyChanged(nameof(TotalAmount));
        }

        private void StartNewPurchase()
        {
            _selectedPurchaseVersion++;
            _selectedPurchase = null;
            OnPropertyChanged(nameof(SelectedPurchase));

            ClearCurrentPurchase();
            SetStatus("Ready for a new purchase.", true);
        }

        private void BeginEdit()
        {
            if (!IsLoadedPurchase)
            {
                SetStatus("Load a saved purchase before editing.", false);
                return;
            }

            _snapshot ??= CreateSnapshot();
            IsInEditMode = true;
            SetStatus($"Purchase {PurchaseNumber} is now editable.", true);
        }

        private void ResetEditor()
        {
            if (IsLoadedPurchase && _snapshot != null)
            {
                PurchaseId = _snapshot.PurchaseId;
                PurchaseNumber = _snapshot.PurchaseNumber;
                SupplierId = _snapshot.SupplierId;
                WarehouseId = _snapshot.WarehouseId;
                PurchaseDate = _snapshot.PurchaseDate;
                PaidAmount = _snapshot.PaidAmount;
                Replace(Items, _snapshot.Items.Select(item => new PurchaseEditorItemViewModel
                {
                    PartId = item.PartId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost
                }));

                IsInEditMode = false;
                OnPropertyChanged(nameof(TotalAmount));
                SetStatus($"Purchase {PurchaseNumber} was restored to its saved values.", true);
                return;
            }

            ClearCurrentPurchase();
            SetStatus("Purchase editor reset.", true);
        }

        private void AddItem()
        {
            if (!IsEditorEnabled)
            {
                SetStatus("Switch to edit mode before changing purchase lines.", false);
                return;
            }

            if (NewPartId is not > 0)
            {
                SetStatus("Select a part first.", false);
                return;
            }

            if (NewQuantity <= 0)
            {
                SetStatus("Quantity must be greater than zero.", false);
                return;
            }

            if (NewUnitCost <= 0)
            {
                SetStatus("Unit cost must be greater than zero.", false);
                return;
            }

            Items.Add(new PurchaseEditorItemViewModel
            {
                PartId = NewPartId.Value,
                Description = NewPartDescription,
                Quantity = NewQuantity,
                UnitCost = NewUnitCost
            });

            NewPartId = null;
            NewPartDescription = string.Empty;
            NewQuantity = 1;
            NewUnitCost = 0m;
            OnPropertyChanged(nameof(TotalAmount));
        }

        private void RemoveSelectedItem()
        {
            if (!CanRemoveItem || SelectedItem == null)
            {
                SetStatus("Select a line to remove first.", false);
                return;
            }

            Items.Remove(SelectedItem);
            SelectedItem = null;
            OnPropertyChanged(nameof(TotalAmount));
        }

        private async Task SubmitAsync()
        {
            if (SupplierId is not > 0)
            {
                SetStatus("Select a supplier first.", false);
                return;
            }

            if (WarehouseId is not > 0)
            {
                SetStatus("Select a warehouse first.", false);
                return;
            }

            if (PaidAmount < 0)
            {
                SetStatus("Paid amount cannot be negative.", false);
                return;
            }

            if (Items.Count == 0)
            {
                SetStatus("Add at least one line before saving the purchase.", false);
                return;
            }

            if (Items.Any(item => item.PartId <= 0 || item.Quantity <= 0 || item.UnitCost <= 0))
            {
                SetStatus("Every purchase line must include a valid part, quantity, and unit cost.", false);
                return;
            }

            try
            {
                if (IsLoadedPurchase)
                {
                    if (!IsInEditMode || PurchaseId is not > 0)
                    {
                        SetStatus("Switch to edit mode before saving changes.", false);
                        return;
                    }

                    await _purchasesApi.UpdatePurchaseAsync(PurchaseId.Value, BuildUpdateRequest());
                    AppNotificationCenter.Instance.Publish($"✓ Purchase {PurchaseNumber} updated.", true);
                    await LoadAsync(PurchaseId.Value);
                    return;
                }

                var result = await _purchasesApi.CreatePurchaseAsync(BuildCreateRequest());
                AppNotificationCenter.Instance.Publish($"✓ Purchase {result.PurchaseNumber} created.", true);
                await LoadAsync(result.PurchaseId);
            }
            catch (Exception ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
                SetStatus($"Saving purchase failed: {ex.Message}", false);
            }
        }

        private CreatePurchaseRequest BuildCreateRequest()
            => new()
            {
                PurchaseDate = PurchaseDate,
                SupplierId = SupplierId ?? 0,
                WarehouseId = WarehouseId ?? 0,
                PaidAmount = PaidAmount,
                Items = Items.Select(ToDto).ToList()
            };

        private UpdatePurchaseRequest BuildUpdateRequest()
            => new()
            {
                PurchaseDate = PurchaseDate,
                SupplierId = SupplierId ?? 0,
                WarehouseId = WarehouseId ?? 0,
                PaidAmount = PaidAmount,
                Items = Items.Select(ToDto).ToList()
            };

        private static PurchaseItemDto ToDto(PurchaseEditorItemViewModel item)
            => new()
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                TaxRate = 0m
            };

        private void ClearCurrentPurchase()
        {
            PurchaseId = null;
            PurchaseNumber = string.Empty;
            SupplierId = Suppliers.FirstOrDefault()?.Id;
            WarehouseId = null;
            PurchaseDate = DateTime.Today;
            PaidAmount = 0m;
            Replace(Items, Array.Empty<PurchaseEditorItemViewModel>());
            SelectedItem = null;
            NewPartId = null;
            NewPartDescription = string.Empty;
            NewQuantity = 1;
            NewUnitCost = 0m;
            IsInEditMode = false;
            _snapshot = null;
            PostingTimeline.Clear();
            OnPropertyChanged(nameof(HasPostingTimeline));
            OnPropertyChanged(nameof(TotalAmount));
        }

        private PurchaseEditorSnapshot CreateSnapshot()
            => new()
            {
                PurchaseId = PurchaseId,
                PurchaseNumber = PurchaseNumber,
                SupplierId = SupplierId,
                WarehouseId = WarehouseId,
                PurchaseDate = PurchaseDate,
                PaidAmount = PaidAmount,
                Items = Items.Select(item => new PurchaseEditorSnapshotLine
                {
                    PartId = item.PartId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost
                }).ToList()
            };

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PurchaseEditorItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PurchaseEditorItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            OnPropertyChanged(nameof(TotalAmount));
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PurchaseEditorItemViewModel.Quantity)
                || e.PropertyName == nameof(PurchaseEditorItemViewModel.UnitCost)
                || e.PropertyName == nameof(PurchaseEditorItemViewModel.LineTotal))
            {
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        private void RaiseModeProperties()
        {
            OnPropertyChanged(nameof(IsLoadedPurchase));
            OnPropertyChanged(nameof(IsEditorEnabled));
            OnPropertyChanged(nameof(IsItemsGridReadOnly));
            OnPropertyChanged(nameof(IsEditEnabled));
            OnPropertyChanged(nameof(CanRemoveItem));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(EditorSubtitle));
        }

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed;
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void HandleBackgroundException(Exception ex)
            => SetStatus(ex.Message, false);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
