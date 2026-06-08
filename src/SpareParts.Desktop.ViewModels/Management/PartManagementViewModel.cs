using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.Parts;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Pricing;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class PartManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private readonly IFilePickerService _filePickerService;
        private readonly IUserNotificationService _notificationService;
        private readonly IPartWorkspaceService _partWorkspaceService;
        private string _newPartCode = string.Empty;
        private string _newPartBarcode = string.Empty;
        private string _newPartName = string.Empty;
        private string _newPartOEM = string.Empty;
        private decimal _newPartCostPrice;
        private decimal _newPartSalePrice;
        private string _newPartAveragePrice = string.Empty;
        private string _newPartEstimatedMarketPrice = string.Empty;
        private string _newPartCurrency = "USD";
        private int _newPartMinStock;
        private int _newPartCategoryId = 1;
        private int? _newPartBrandId;
        private string _newPartNotes = string.Empty;
        private PartDto? _selectedPart;
        private SmartPricingCoachResult _pricingCoach = SmartPricingCoach.Evaluate(null);
        private bool _isGeneratingPartNotes;
        private bool _isImportingParts;

        public PartManagementViewModel(
            IManagementFeatureContext context,
            IFilePickerService filePickerService,
            IUserNotificationService notificationService,
            IPartWorkspaceService partWorkspaceService)
        {
            _ctx = context;
            _filePickerService = filePickerService;
            _notificationService = notificationService;
            _partWorkspaceService = partWorkspaceService;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ = ImportFromExcelAsync());
            GeneratePartNotesCommand = new RelayCommand(_ => _ = GeneratePartNotesAsync());
            GenerateListingCommand = new RelayCommand(_ => OpenPartListingPackage());
        }

        public ObservableCollection<PartDto> Parts { get; } = new BulkObservableCollection<PartDto>();
        public ObservableCollection<CategoryDto> Categories { get; } = new BulkObservableCollection<CategoryDto>();
        public ObservableCollection<BrandDto> BrandOptions { get; } = new BulkObservableCollection<BrandDto>();
        public ObservableCollection<CurrencyRateDto> CurrencyRates { get; } = new BulkObservableCollection<CurrencyRateDto>();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }
        public ICommand GeneratePartNotesCommand { get; }
        public ICommand GenerateListingCommand { get; }

        public bool IsGeneratingPartNotes
        {
            get => _isGeneratingPartNotes;
            private set
            {
                if (!SetProperty(ref _isGeneratingPartNotes, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(PartAiButtonText));
            }
        }

        public bool IsImportingParts
        {
            get => _isImportingParts;
            private set
            {
                if (!SetProperty(ref _isImportingParts, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(PartImportButtonText));
            }
        }

        public string PartAiButtonText => IsGeneratingPartNotes ? "AI is drafting notes..." : "✨ Draft Notes with AI";
        public string PartImportButtonText => IsImportingParts ? "Importing workbook..." : "⬆ Import Parts from Excel";
        public string SmartPricingCoachMessage => _pricingCoach.Message;
        public string SmartPricingCoachBadge => _pricingCoach.Badge;
        public string SmartPricingCoachTone => _pricingCoach.Tone;


        public void LoadReferenceData(
            IEnumerable<CategoryDto> categories,
            IEnumerable<BrandDto> brands,
            IEnumerable<CurrencyRateDto> currencyRates)
        {
            Replace(Categories, categories);
            Replace(BrandOptions, brands);
            Replace(CurrencyRates, currencyRates);
        }

        public void LoadParts(
            IEnumerable<PartDto> parts,
            IEnumerable<PartRequestDto> _)
        {
            Replace(Parts, parts);
        }

        public string NewPartCode
        {
            get => _newPartCode;
            set => SetProperty(ref _newPartCode, value);
        }

        public string NewPartBarcode
        {
            get => _newPartBarcode;
            set => SetProperty(ref _newPartBarcode, value);
        }

        public string NewPartName
        {
            get => _newPartName;
            set => SetProperty(ref _newPartName, value);
        }

        public string NewPartOEM
        {
            get => _newPartOEM;
            set => SetProperty(ref _newPartOEM, value);
        }

        public decimal NewPartCostPrice
        {
            get => _newPartCostPrice;
            set
            {
                if (SetProperty(ref _newPartCostPrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public decimal NewPartSalePrice
        {
            get => _newPartSalePrice;
            set
            {
                if (SetProperty(ref _newPartSalePrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public string NewPartAveragePrice
        {
            get => _newPartAveragePrice;
            set
            {
                if (SetProperty(ref _newPartAveragePrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public string NewPartEstimatedMarketPrice
        {
            get => _newPartEstimatedMarketPrice;
            set
            {
                if (SetProperty(ref _newPartEstimatedMarketPrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public string NewPartCurrency
        {
            get => _newPartCurrency;
            set
            {
                if (SetProperty(ref _newPartCurrency, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public int NewPartMinStock
        {
            get => _newPartMinStock;
            set
            {
                if (SetProperty(ref _newPartMinStock, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public int NewPartCategoryId
        {
            get => _newPartCategoryId;
            set => SetProperty(ref _newPartCategoryId, value);
        }

        public int? NewPartBrandId
        {
            get => _newPartBrandId;
            set => SetProperty(ref _newPartBrandId, value);
        }

        public string NewPartNotes
        {
            get => _newPartNotes;
            set => SetProperty(ref _newPartNotes, value);
        }

        public PartDto? SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (!SetProperty(ref _selectedPart, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
                else
                {
                    UpdatePricingCoach();
                }
            }
        }

        public void PopulateForm(PartDto p)
        {
            NewPartCode = p.InternalCode;
            NewPartBarcode = p.Barcode ?? string.Empty;
            NewPartName = p.Name;
            NewPartOEM = p.OEMNumber ?? string.Empty;
            NewPartCategoryId = p.CategoryId;
            NewPartBrandId = p.BrandId;
            NewPartCostPrice = p.CostPrice;
            NewPartSalePrice = p.SalePrice;
            NewPartAveragePrice = p.AveragePrice?.ToString("0.##") ?? string.Empty;
            NewPartEstimatedMarketPrice = p.EstimatedMarketPrice?.ToString("0.##") ?? string.Empty;
            NewPartCurrency = p.Currency;
            NewPartMinStock = p.MinStock;
            NewPartNotes = p.Notes ?? string.Empty;
            UpdatePricingCoach();
        }

        public void ClearForm(string defaultCurrencyCode = "USD")
        {
            NewPartCode = NewPartBarcode = NewPartName = NewPartOEM = NewPartAveragePrice = NewPartEstimatedMarketPrice = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            NewPartCurrency = defaultCurrencyCode;
            NewPartMinStock = 0;
            NewPartCategoryId = 1;
            NewPartBrandId = null;
            SelectedPart = null;
            UpdatePricingCoach();
        }

        public void StartNew() => ClearForm(_ctx.GetDefaultCurrencyCode());

        private async Task SaveAsync()
        {
            var result = await _ctx.Coordinator.SavePartAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            var result = await _ctx.Coordinator.DeletePartAsync(SelectedPart);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }

        private void OpenPartListingPackage()
        {
            if (SelectedPart is not { Id: > 0 } part)
            {
                _notificationService.Show("Select a part row first, then generate its listing package.", "Listing Package", NotificationKind.Warning);
                return;
            }

            _partWorkspaceService.OpenListingPackage(new PartWorkspaceRequest
            {
                PartId = part.Id,
                PartName = part.Name
            });
        }

        private async Task GeneratePartNotesAsync()
        {
            if (IsGeneratingPartNotes) return;
            IsGeneratingPartNotes = true;
            _ctx.SetStatus("Drafting part notes with AI…", true);
            try
            {
                var categoryLookup = Categories.ToDictionary(item => item.Id, item => item.Name);
                var brandLookup = BrandOptions.ToDictionary(item => item.Id, item => item.Name);
                var result = await _ctx.Coordinator.GeneratePartNotesAsync(this, categoryLookup, brandLookup);
                _ctx.SetStatus(result.Message, result.Success);
            }
            finally
            {
                IsGeneratingPartNotes = false;
            }
        }

        private async Task ImportFromExcelAsync()
        {
            if (IsImportingParts) return;

            var filePath = _filePickerService
                .PickFiles(new FilePickerRequest
                {
                    Filter = "Excel workbook|*.xlsx",
                    AllowMultiple = false,
                    Title = "Import parts from Excel"
                })
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsImportingParts = true;
            _ctx.SetStatus("Importing parts from Excel…", true);
            try
            {
                var result = await _ctx.Coordinator.ImportPartsFromExcelAsync(filePath, Categories.ToList(), BrandOptions.ToList());
                _ctx.SetStatus(result.SummaryMessage, result.HasImportedRows);

                if (result.HasImportedRows)
                {
                    await _ctx.RefreshAsync();
                    StartNew();
                }

                _notificationService.Show(
                    result.ToDialogMessage(),
                    "Parts Import",
                    result.HasErrors
                        ? (result.HasImportedRows ? NotificationKind.Warning : NotificationKind.Error)
                        : NotificationKind.Success);
            }
            finally
            {
                IsImportingParts = false;
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
            => target.ReplaceWith(source);

        private void UpdatePricingCoach()
        {
            var averagePrice = SmartPricingCoach.ParseAveragePrice(NewPartEstimatedMarketPrice)
                ?? SmartPricingCoach.ParseAveragePrice(NewPartAveragePrice);
            var availableQuantity = SelectedPart?.AvailableQuantity ?? SelectedPart?.StockQuantity ?? 0;
            _pricingCoach = SmartPricingCoach.Evaluate(
                NewPartCostPrice,
                NewPartSalePrice,
                averagePrice,
                NewPartCurrency,
                availableQuantity,
                NewPartMinStock);
            OnPropertyChanged(nameof(SmartPricingCoachMessage));
            OnPropertyChanged(nameof(SmartPricingCoachBadge));
            OnPropertyChanged(nameof(SmartPricingCoachTone));
        }
    }
}
