using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Pricing;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Sales;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Threading;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class InvoiceTabsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<BrandGroupViewModel> BrandGroups    { get; } = new();
        public ObservableCollection<CarModelViewModel>   AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel>        AvailableParts { get; } = new();
        public ObservableCollection<StatusMessage> Notifications { get; } = AppNotificationCenter.Instance.Messages;

        private readonly ICarCatalogApiClient _carCatalogApi;
        private readonly IPartsApiClient _partsApi;
        private readonly ISalesApiClient _salesApi;
        private readonly ICrudApiClient _crudApi;
        private readonly IRoleApiClient _rolesApi;
        private readonly IArRenderingService _arRenderingService;
        private readonly IArDeviceBridge _arDeviceBridge;
        private int _invoiceSearchVersion;
        private CancellationTokenSource? _invoiceSearchCts;
        private int _carsLoadVersion;

        public ManagementViewModel ManagementVm { get; }
        public PartPurchasesViewModel PartPurchasesVm { get; }
        public UsedCarPurchasesViewModel PurchasesVm { get; }
        public UsedCarWholesaleViewModel UsedCarWholesaleVm { get; }
        public RepairPrepBoardViewModel RepairPrepVm { get; }
        public ReportBuilderViewModel ReportBuilderVm { get; }
        public OwnerCockpitDashboardViewModel OwnerCockpitVm { get; }
        public BusinessAssistantViewModel BusinessAssistantVm { get; }
        public WhatsAppInboxViewModel WhatsAppVm { get; }
        public BarcodeModeViewModel BarcodeModeVm { get; }
        public PartCompatibilityViewModel PartCompatibilityVm { get; }
        public DeadStockResurrectionViewModel DeadStockVm { get; }
        public StockArrivalTheaterViewModel StockArrivalVm { get; }

        private bool _canViewInvoiceSearch;
        public bool CanViewInvoiceSearch
        {
            get => _canViewInvoiceSearch;
            private set { _canViewInvoiceSearch = value; OnPropertyChanged(nameof(CanViewInvoiceSearch)); }
        }

        private bool _canCreateInvoice;
        public bool CanCreateInvoice
        {
            get => _canCreateInvoice;
            private set { _canCreateInvoice = value; OnPropertyChanged(nameof(CanCreateInvoice)); }
        }

        private bool _canViewManagementScreen;
        public bool CanViewManagementScreen
        {
            get => _canViewManagementScreen;
            private set { _canViewManagementScreen = value; OnPropertyChanged(nameof(CanViewManagementScreen)); }
        }

        private bool _canViewPosScreen;
        public bool CanViewPosScreen
        {
            get => _canViewPosScreen;
            private set { _canViewPosScreen = value; OnPropertyChanged(nameof(CanViewPosScreen)); }
        }

        private bool _canViewPurchasesScreen;
        public bool CanViewPurchasesScreen
        {
            get => _canViewPurchasesScreen;
            private set { _canViewPurchasesScreen = value; OnPropertyChanged(nameof(CanViewPurchasesScreen)); }
        }

        private bool _canViewStockManagementScreen;
        public bool CanViewStockManagementScreen
        {
            get => _canViewStockManagementScreen;
            private set { _canViewStockManagementScreen = value; OnPropertyChanged(nameof(CanViewStockManagementScreen)); }
        }

        private bool _canViewStockArrivalScreen;
        public bool CanViewStockArrivalScreen
        {
            get => _canViewStockArrivalScreen;
            private set { _canViewStockArrivalScreen = value; OnPropertyChanged(nameof(CanViewStockArrivalScreen)); }
        }

        private bool _canViewAccountingScreen;
        public bool CanViewAccountingScreen
        {
            get => _canViewAccountingScreen;
            private set { _canViewAccountingScreen = value; OnPropertyChanged(nameof(CanViewAccountingScreen)); }
        }

        private bool _canViewManualJournalScreen;
        public bool CanViewManualJournalScreen
        {
            get => _canViewManualJournalScreen;
            private set { _canViewManualJournalScreen = value; OnPropertyChanged(nameof(CanViewManualJournalScreen)); }
        }

        private bool _canViewReportBuilderScreen;
        public bool CanViewReportBuilderScreen
        {
            get => _canViewReportBuilderScreen;
            private set { _canViewReportBuilderScreen = value; OnPropertyChanged(nameof(CanViewReportBuilderScreen)); }
        }

        private bool _canViewBusinessAssistantScreen;
        public bool CanViewBusinessAssistantScreen
        {
            get => _canViewBusinessAssistantScreen;
            private set { _canViewBusinessAssistantScreen = value; OnPropertyChanged(nameof(CanViewBusinessAssistantScreen)); }
        }

        private bool _canViewWhatsAppScreen;
        public bool CanViewWhatsAppScreen
        {
            get => _canViewWhatsAppScreen;
            private set { _canViewWhatsAppScreen = value; OnPropertyChanged(nameof(CanViewWhatsAppScreen)); }
        }

        private bool _canViewCarSelectionScreen;
        public bool CanViewCarSelectionScreen
        {
            get => _canViewCarSelectionScreen;
            private set { _canViewCarSelectionScreen = value; OnPropertyChanged(nameof(CanViewCarSelectionScreen)); }
        }

        private bool _canViewPartSelectionScreen;
        public bool CanViewPartSelectionScreen
        {
            get => _canViewPartSelectionScreen;
            private set { _canViewPartSelectionScreen = value; OnPropertyChanged(nameof(CanViewPartSelectionScreen)); }
        }

        private bool _canViewArScreen;
        public bool CanViewArScreen
        {
            get => _canViewArScreen;
            private set { _canViewArScreen = value; OnPropertyChanged(nameof(CanViewArScreen)); }
        }

        private bool _canViewBarcodeQrScreen;
        public bool CanViewBarcodeQrScreen
        {
            get => _canViewBarcodeQrScreen;
            private set { _canViewBarcodeQrScreen = value; OnPropertyChanged(nameof(CanViewBarcodeQrScreen)); }
        }

        private bool _isManagementOpen;
        public bool IsManagementOpen
        {
            get => _isManagementOpen;
            set { _isManagementOpen = value; OnPropertyChanged(nameof(IsManagementOpen)); }
        }

        private bool _isFeedVisible = true;
        public bool IsFeedVisible
        {
            get => _isFeedVisible;
            set
            {
                if (_isFeedVisible == value) return;
                _isFeedVisible = value;
                OnPropertyChanged(nameof(IsFeedVisible));
                OnPropertyChanged(nameof(FeedToggleText));
            }
        }

        public string FeedToggleText => IsFeedVisible ? "Hide" : "Show";

        private bool _isArSessionActive;
        public bool IsArSessionActive
        {
            get => _isArSessionActive;
            private set { _isArSessionActive = value; OnPropertyChanged(nameof(IsArSessionActive)); }
        }

        private string _arStatusMessage = "AR service idle.";
        public string ArStatusMessage
        {
            get => _arStatusMessage;
            private set { _arStatusMessage = value; OnPropertyChanged(nameof(ArStatusMessage)); }
        }

        private string _arOverlayTitle = "No overlay yet.";
        public string ArOverlayTitle
        {
            get => _arOverlayTitle;
            private set { _arOverlayTitle = value; OnPropertyChanged(nameof(ArOverlayTitle)); }
        }

        private string _arOverlayDiagnostic = string.Empty;
        public string ArOverlayDiagnostic
        {
            get => _arOverlayDiagnostic;
            private set { _arOverlayDiagnostic = value; OnPropertyChanged(nameof(ArOverlayDiagnostic)); }
        }

        private string _arReferenceImages = "No reference images yet.";
        public string ArReferenceImages
        {
            get => _arReferenceImages;
            private set { _arReferenceImages = value; OnPropertyChanged(nameof(ArReferenceImages)); }
        }

        private double _arOverlayLeft = 80;
        public double ArOverlayLeft
        {
            get => _arOverlayLeft;
            private set { _arOverlayLeft = value; OnPropertyChanged(nameof(ArOverlayLeft)); }
        }

        private double _arOverlayTop = 80;
        public double ArOverlayTop
        {
            get => _arOverlayTop;
            private set { _arOverlayTop = value; OnPropertyChanged(nameof(ArOverlayTop)); }
        }

        private BitmapImage? _arOverlayPreviewImage;
        public BitmapImage? ArPreviewImage => SelectedCar?.Image ?? SelectedBrand?.Logo ?? _arOverlayPreviewImage;

        private CarBrandViewModel? _selectedBrand;
        public CarBrandViewModel? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                _selectedBrand = value;
                OnPropertyChanged(nameof(SelectedBrand));
                OnPropertyChanged(nameof(ArPreviewImage));
            }
        }

        private CarModelViewModel? _selectedCar;
        public CarModelViewModel? SelectedCar
        {
            get => _selectedCar;
            set
            {
                _selectedCar = value;
                OnPropertyChanged(nameof(SelectedCar));
                OnPropertyChanged(nameof(ArPreviewImage));
            }
        }

        private AppScreen _activeScreen = AppScreen.HomePage;
        public AppScreen ActiveScreen
        {
            get => _activeScreen;
            set { if (_activeScreen != value) { _activeScreen = value; OnPropertyChanged(nameof(ActiveScreen)); } }
        }

        private bool _isInvoiceSearchOpen;
        public bool IsInvoiceSearchOpen
        {
            get => _isInvoiceSearchOpen;
            set { _isInvoiceSearchOpen = value; OnPropertyChanged(nameof(IsInvoiceSearchOpen)); }
        }

        private string _invoiceSearchText = string.Empty;
        public string InvoiceSearchText
        {
            get => _invoiceSearchText;
            set
            {
                if (_invoiceSearchText == value) return;
                _invoiceSearchText = value;
                OnPropertyChanged(nameof(InvoiceSearchText));
                RefreshInvoiceSearch();
            }
        }

        public ObservableCollection<SalesInvoiceLookupDto> InvoiceSearchResults { get; } = new();

        private bool _isLoadingInvoiceSearch;
        public bool IsLoadingInvoiceSearch
        {
            get => _isLoadingInvoiceSearch;
            set
            {
                _isLoadingInvoiceSearch = value;
                OnPropertyChanged(nameof(IsLoadingInvoiceSearch));
                OnPropertyChanged(nameof(IsGlobalLoading));
            }
        }

        private bool _isLoadingBrands;
        public bool IsLoadingBrands
        {
            get => _isLoadingBrands;
            set
            {
                _isLoadingBrands = value;
                OnPropertyChanged(nameof(IsLoadingBrands));
                OnPropertyChanged(nameof(IsGlobalLoading));
            }
        }

        private bool _isLoadingCars;
        public bool IsLoadingCars
        {
            get => _isLoadingCars;
            set
            {
                _isLoadingCars = value;
                OnPropertyChanged(nameof(IsLoadingCars));
                OnPropertyChanged(nameof(IsGlobalLoading));
            }
        }

        private bool _isLoadingParts;
        public bool IsLoadingParts
        {
            get => _isLoadingParts;
            set
            {
                _isLoadingParts = value;
                OnPropertyChanged(nameof(IsLoadingParts));
                OnPropertyChanged(nameof(IsGlobalLoading));
            }
        }

        private bool _isLoadingRolePermissions;
        public bool IsLoadingRolePermissions
        {
            get => _isLoadingRolePermissions;
            set
            {
                _isLoadingRolePermissions = value;
                OnPropertyChanged(nameof(IsLoadingRolePermissions));
                OnPropertyChanged(nameof(IsGlobalLoading));
            }
        }

        private bool _isLoadingInvoiceOpen;
        public bool IsLoadingInvoiceOpen
        {
            get => _isLoadingInvoiceOpen;
            set
            {
                _isLoadingInvoiceOpen = value;
                OnPropertyChanged(nameof(IsLoadingInvoiceOpen));
                OnPropertyChanged(nameof(IsGlobalLoading));
            }
        }

        public bool IsGlobalLoading =>
            IsLoadingBrands ||
            IsLoadingCars ||
            IsLoadingParts ||
            IsLoadingRolePermissions ||
            IsLoadingInvoiceSearch ||
            IsLoadingInvoiceOpen ||
            OwnerCockpitVm.IsLoading ||
            BusinessAssistantVm.IsLoading ||
            WhatsAppVm.IsLoading ||
            BarcodeModeVm.IsLoading ||
            DeadStockVm.IsLoading ||
            StockArrivalVm.IsLoading ||
            PartPurchasesVm.IsLoading ||
            PurchasesVm.IsLoading ||
            RepairPrepVm.IsLoading ||
            PartCompatibilityVm.IsLoading ||
            ReportBuilderVm.IsLoading ||
            ManagementVm.IsLoading ||
            ManagementVm.AccountingVm.IsLoading;

        public ObservableCollection<ThemeOption> Themes { get; } = new();
        public ICommand SelectThemeCommand { get; private set; } = null!;

        public ObservableCollection<InvoiceTabViewModel> Tabs { get; } = new();
        public ObservableCollection<PurchaseDraftItemViewModel> PurchaseDraftItems { get; } = new();
        public ObservableCollection<StockSnapshotViewModel> StockSnapshots { get; } = new();

        private InvoiceTabViewModel? _selectedTab;
        public InvoiceTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(nameof(SelectedTab)); }
        }

        public ICommand AddTabCommand            { get; }
        public ICommand CloseTabCommand          { get; }
        public ICommand SelectBrandCommand       { get; }
        public ICommand SelectCarCommand         { get; }
        public ICommand SelectPartCommand        { get; }
        public ICommand GoToPosCommand           { get; }
        public ICommand GoToCarSelectionCommand  { get; }
        public ICommand GoToHomeCommand          { get; }
        public ICommand CreateInvoiceCommand    { get; }
        public ICommand OpenManagementCommand    { get; }
        public ICommand OpenInvoiceSearchCommand { get; }
        public ICommand ReloadInvoiceSearchCommand { get; }
        public ICommand GoToPurchasesCommand     { get; }
        public ICommand GoToUsedCarPurchasesCommand { get; }
        public ICommand GoToUsedCarWholesaleCommand { get; }
        public ICommand GoToPurchaseHistoryCommand { get; }
        public ICommand GoToStockArrivalCommand { get; }
        public ICommand GoToRepairPrepCommand { get; }
        public ICommand GoToStockManagementCommand { get; }
        public ICommand GoToDeadStockCommand { get; }
        public ICommand GoToCompatibilityCommand { get; }
        public ICommand GoToAccountingCommand { get; }
        public ICommand GoToManualJournalCommand { get; }
        public ICommand GoToReportBuilderCommand { get; }
        public ICommand GoToWhatsAppCommand { get; }
        public ICommand GoToBusinessAssistantCommand { get; }
        public ICommand GoToBarcodeModeCommand { get; }
        public ICommand GoToArCommand { get; }
        public ICommand StartArSessionCommand { get; }
        public ICommand StopArSessionCommand { get; }
        public ICommand ToggleFeedCommand        { get; }

        public InvoiceTabsViewModel(
            ICarCatalogApiClient carCatalogApi,
            IPartsApiClient partsApi,
            IAccountingApiClient accountingApi,
            IPurchasesApiClient purchasesApi,
            ISalesApiClient salesApi,
            ICrudApiClient crudApi,
            IRoleApiClient rolesApi,
            IWarehouseApiClient warehouseApi,
            IOwnerCockpitApiClient ownerCockpitApi,
            IBusinessAssistantApiClient businessAssistantApi,
            IReportBuilderApiClient reportBuilderApi,
            IArRenderingService arRenderingService,
            IArDeviceBridge arDeviceBridge,
            ManagementViewModel managementVm)
        {
            _carCatalogApi = carCatalogApi;
            _partsApi = partsApi;
            _salesApi = salesApi;
            _crudApi = crudApi;
            _rolesApi = rolesApi;
            _arRenderingService = arRenderingService;
            _arDeviceBridge = arDeviceBridge;
            ManagementVm = managementVm;
            OwnerCockpitVm = new OwnerCockpitDashboardViewModel(ownerCockpitApi);
            BusinessAssistantVm = new BusinessAssistantViewModel(businessAssistantApi);
            WhatsAppVm = new WhatsAppInboxViewModel(crudApi);
            BarcodeModeVm = new BarcodeModeViewModel(partsApi, salesApi, crudApi, warehouseApi);
            DeadStockVm = new DeadStockResurrectionViewModel(partsApi);
            PartPurchasesVm = new PartPurchasesViewModel(crudApi, purchasesApi);
            PurchasesVm = new UsedCarPurchasesViewModel(crudApi, accountingApi, purchasesApi);
            UsedCarWholesaleVm = new UsedCarWholesaleViewModel(crudApi);
            StockArrivalVm = new StockArrivalTheaterViewModel(crudApi, NavigateFromStockArrival);
            RepairPrepVm = new RepairPrepBoardViewModel(crudApi);
            PartCompatibilityVm = new PartCompatibilityViewModel(crudApi);
            ReportBuilderVm = new ReportBuilderViewModel(reportBuilderApi);
            ManagementVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ManagementViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            ManagementVm.AccountingVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(Management.AccountingViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            OwnerCockpitVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(OwnerCockpitDashboardViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            BusinessAssistantVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(BusinessAssistantViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            WhatsAppVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(WhatsAppInboxViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            BarcodeModeVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(BarcodeModeViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            DeadStockVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(DeadStockResurrectionViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            PartPurchasesVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PartPurchasesViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            PurchasesVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(UsedCarPurchasesViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            StockArrivalVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(StockArrivalTheaterViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            RepairPrepVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(RepairPrepBoardViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            PartCompatibilityVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PartCompatibilityViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };
            ReportBuilderVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ReportBuilderViewModel.IsLoading))
                {
                    OnPropertyChanged(nameof(IsGlobalLoading));
                }
            };

            Themes.Add(new ThemeOption { Key = AppTheme.Default,       Name = "Default",       SubTitle = "Sport Orange · Dark",       AccentHex = "#FF5722" });
            Themes.Add(new ThemeOption { Key = AppTheme.MPower,        Name = "M Power",       SubTitle = "BMW · Midnight Blue",        AccentHex = "#1C69D4" });
            Themes.Add(new ThemeOption { Key = AppTheme.NeonGlow,      Name = "Neon Glow",     SubTitle = "Cyberpunk · Electric Cyan",  AccentHex = "#00E5FF" });
            Themes.Add(new ThemeOption { Key = AppTheme.AMG,           Name = "AMG",           SubTitle = "Mercedes · Titanium Grey",   AccentHex = "#C0C0C0" });
            Themes.Add(new ThemeOption { Key = AppTheme.PorscheRS,     Name = "Porsche RS",    SubTitle = "Racing · Guards Red",        AccentHex = "#E30613" });
            Themes.Add(new ThemeOption { Key = AppTheme.LamborghiniSC, Name = "Squadra Corse", SubTitle = "Lamborghini · Giallo Orion", AccentHex = "#FFD600" });

            SelectThemeCommand = new RelayCommand(o =>
            {
                if (o is not ThemeOption picked) return;
                foreach (var t in Themes) t.IsSelected = false;
                picked.IsSelected = true;
                ThemeManager.ApplyTheme(picked.Key);
            });
            Themes[0].IsSelected = true;
            ThemeManager.ApplyTheme(AppTheme.Default);

            SelectBrandCommand      = new RelayCommand(SelectBrand);
            SelectCarCommand        = new RelayCommand(SelectCar);
            SelectPartCommand       = new RelayCommand(SelectPart);
            GoToPosCommand          = new RelayCommand(_ =>
            {
                if (!CanViewPosScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the POS screen.", false);
                    return;
                }

                ActiveScreen = AppScreen.Pos;
            });
            GoToCarSelectionCommand = new RelayCommand(_ =>
            {
                if (!CanViewCarSelectionScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view car selection.", false);
                    return;
                }

                ActiveScreen = AppScreen.CarSelection;
            });
            GoToHomeCommand         = new RelayCommand(_ =>
            {
                ActiveScreen  = AppScreen.HomePage;
                SelectedBrand = null;
                AvailableCars.Clear();
                OwnerCockpitVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });

            OpenManagementCommand = new RelayCommand(_ =>
            {
                if (!CanViewManagementScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the management screen.", false);
                    return;
                }

                IsManagementOpen = !IsManagementOpen;
                if (IsManagementOpen)
                    ManagementVm.LoadAllAsync().SafeFireAndForget(HandleBackgroundException);
            });

            OpenInvoiceSearchCommand = new RelayCommand(_ =>
            {
                if (!CanViewInvoiceSearch)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view invoice search.", false);
                    return;
                }

                IsInvoiceSearchOpen = !IsInvoiceSearchOpen;
                if (IsInvoiceSearchOpen)
                {
                    RefreshInvoiceSearch();
                }
            });
            ReloadInvoiceSearchCommand = new RelayCommand(_ => RefreshInvoiceSearch());

            GoToPurchasesCommand = new RelayCommand(_ =>
            {
                if (!CanViewPurchasesScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the purchases screen.", false);
                    return;
                }

                ActiveScreen = AppScreen.PartPurchases;
                PartPurchasesVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToUsedCarPurchasesCommand = new RelayCommand(_ =>
            {
                if (!CanViewPurchasesScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view used-car purchases.", false);
                    return;
                }

                ActiveScreen = AppScreen.Purchases;
                PurchasesVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToUsedCarWholesaleCommand = new RelayCommand(_ =>
            {
                if (!CanViewPurchasesScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view used-car wholesale.", false);
                    return;
                }

                ActiveScreen = AppScreen.UsedCarWholesale;
                UsedCarWholesaleVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToPurchaseHistoryCommand = new RelayCommand(_ =>
            {
                if (!CanViewPurchasesScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view used-car purchase history.", false);
                    return;
                }

                ActiveScreen = AppScreen.PurchaseHistory;
                PurchasesVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToStockArrivalCommand = new RelayCommand(_ =>
            {
                if (!CanViewStockArrivalScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view stock arrival opportunities.", false);
                    return;
                }

                ActiveScreen = AppScreen.StockArrivalTheater;
                StockArrivalVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToRepairPrepCommand = new RelayCommand(_ =>
            {
                if (!CanViewPurchasesScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view repair/prep.", false);
                    return;
                }

                ActiveScreen = AppScreen.RepairPrepBoard;
                RepairPrepVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToStockManagementCommand = new RelayCommand(_ =>
            {
                if (!CanViewStockManagementScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view stock management.", false);
                    return;
                }

                ActiveScreen = AppScreen.StockManagement;
                LoadStockSnapshotsAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToDeadStockCommand = new RelayCommand(_ =>
            {
                if (!CanViewStockManagementScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view dead stock recovery.", false);
                    return;
                }

                ActiveScreen = AppScreen.DeadStockResurrection;
                DeadStockVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToCompatibilityCommand = new RelayCommand(_ =>
            {
                if (!CanViewStockManagementScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view part compatibility.", false);
                    return;
                }

                ActiveScreen = AppScreen.PartCompatibility;
                PartCompatibilityVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToAccountingCommand = new RelayCommand(_ =>
            {
                if (!CanViewAccountingScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view accounting.", false);
                    return;
                }

                ActiveScreen = AppScreen.Accounting;
                ManagementVm.AccountingVm.LoadReviewAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToManualJournalCommand = new RelayCommand(_ =>
            {
                if (!CanViewManualJournalScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view manual journals.", false);
                    return;
                }

                ActiveScreen = AppScreen.ManualJournal;
                ManagementVm.AccountingVm.LoadManualJournalAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToReportBuilderCommand = new RelayCommand(_ =>
            {
                if (!CanViewReportBuilderScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view Report Builder.", false);
                    return;
                }

                ActiveScreen = AppScreen.ReportBuilder;
                if (ReportBuilderVm.Tables.Count == 0)
                {
                    ReportBuilderVm.LoadMetadataAsync().SafeFireAndForget(HandleBackgroundException);
                }
            });
            GoToWhatsAppCommand = new RelayCommand(_ =>
            {
                if (!CanViewWhatsAppScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view WhatsApp conversations.", false);
                    return;
                }

                ActiveScreen = AppScreen.WhatsAppInbox;
                WhatsAppVm.LoadConversationsAsync().SafeFireAndForget(HandleBackgroundException);
                WhatsAppVm.LoadCampaignBuilderAsync().SafeFireAndForget(HandleBackgroundException);
            });
            GoToBusinessAssistantCommand = new RelayCommand(_ =>
            {
                if (!CanViewBusinessAssistantScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the business assistant.", false);
                    return;
                }

                ActiveScreen = AppScreen.BusinessAssistant;
            });
            void OpenBarcodeMode()
            {
                if (!CanViewBarcodeQrScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view AR Search.", false);
                    return;
                }

                ActiveScreen = AppScreen.BarcodeMode;
                BarcodeModeVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            }

            GoToBarcodeModeCommand = new RelayCommand(_ => OpenBarcodeMode());
            GoToArCommand = new RelayCommand(_ => OpenBarcodeMode());
            StartArSessionCommand = new RelayCommand(_ => StartArSession());
            StopArSessionCommand = new RelayCommand(_ => StopArSession());
            ToggleFeedCommand = new RelayCommand(_ => IsFeedVisible = !IsFeedVisible);
            CreateInvoiceCommand = new RelayCommand(_ => CreateInvoice());
            AddTabCommand = CreateInvoiceCommand;
            CloseTabCommand = new RelayCommand(o => CloseTab(o as InvoiceTabViewModel));

           // SeedPurchasesAndStock();
            AddTab();
            RefreshInvoiceSearch();
            OwnerCockpitVm.LoadAsync().SafeFireAndForget(HandleBackgroundException);
            LoadBrandsAsync().SafeFireAndForget(HandleBackgroundException);
            LoadRolePermissionsAsync().SafeFireAndForget(HandleBackgroundException);
        }

        private void NavigateFromStockArrival(AppScreen screen)
        {
            switch (screen)
            {
                case AppScreen.RepairPrepBoard:
                    GoToRepairPrepCommand.Execute(null);
                    break;
                case AppScreen.StockManagement:
                    GoToStockManagementCommand.Execute(null);
                    break;
                case AppScreen.WhatsAppInbox:
                    GoToWhatsAppCommand.Execute(null);
                    break;
                case AppScreen.Purchases:
                    GoToUsedCarPurchasesCommand.Execute(null);
                    break;
                case AppScreen.UsedCarWholesale:
                    GoToUsedCarWholesaleCommand.Execute(null);
                    break;
                case AppScreen.PartPurchases:
                    GoToPurchasesCommand.Execute(null);
                    break;
                default:
                    ActiveScreen = screen;
                    break;
            }
        }

        private async Task LoadRolePermissionsAsync()
        {
            IsLoadingRolePermissions = true;
            try
            {
                var roleId = SessionContext.CurrentUser?.RoleId;
                if (!roleId.HasValue || roleId.Value <= 0)
                {
                    ApplyPermissions(new List<RoleMenuAccessDto>());
                    return;
                }

                var permissions = await _rolesApi.GetRoleMenuAccessAsync(roleId.Value);
                ApplyPermissions(permissions);
            }
            catch (Exception)
            {
                ApplyPermissions(new List<RoleMenuAccessDto>());
                AppNotificationCenter.Instance.Publish("✗ Could not load role permissions. Access is restricted.", false);
            }
            finally
            {
                IsLoadingRolePermissions = false;
            }
        }

        private void ApplyPermissions(List<RoleMenuAccessDto> menuAccessItems)
        {
            var invoiceCreate = GetMenuAccess(menuAccessItems, "invoice_create");
            var invoiceSearch = GetMenuAccess(menuAccessItems, "invoice_search");
            var managementScreen = GetMenuAccess(menuAccessItems, "management_screen");
            var supplierTab = GetMenuAccess(menuAccessItems, "supplier_tab");
            var currencyTab = GetMenuAccess(menuAccessItems, "currency_tab");
            var transactionTypesTab = GetMenuAccess(menuAccessItems, "transaction_types_tab");
            var accountingScreen = GetMenuAccess(menuAccessItems, "accounting_screen");
            var manualJournalScreen = GetMenuAccess(menuAccessItems, "manual_journal_screen");
            var reportBuilderScreen = GetMenuAccess(menuAccessItems, "report_builder_screen");
            var posScreen = GetMenuAccess(menuAccessItems, "pos_screen");
            var purchasesScreen = GetMenuAccess(menuAccessItems, "purchases_screen");
            var stockManagementScreen = GetMenuAccess(menuAccessItems, "stock_management_screen");
            var carSelectionScreen = GetMenuAccess(menuAccessItems, "car_selection_screen");
            var partSelectionScreen = GetMenuAccess(menuAccessItems, "part_selection_screen");
            var whatsappScreen = GetMenuAccess(menuAccessItems, "whatsapp_screen");
            var barcodeQrScreen = GetMenuAccess(menuAccessItems, "barcode_qr_screen");
            var hasBarcodeQrScreen = menuAccessItems.Any(i => string.Equals(i.MenuKey, "barcode_qr_screen", StringComparison.OrdinalIgnoreCase));
            var arScreen = menuAccessItems.FirstOrDefault(i => string.Equals(i.MenuKey, "ar_screen", StringComparison.OrdinalIgnoreCase));

            CanViewInvoiceSearch = invoiceSearch.CanView;
            CanCreateInvoice = invoiceCreate.CanView || invoiceCreate.CanEdit;
            CanViewManagementScreen = managementScreen.CanView;
            CanViewPosScreen = posScreen.CanView;
            CanViewPurchasesScreen = purchasesScreen.CanView;
            CanViewStockManagementScreen = stockManagementScreen.CanView;
            CanViewStockArrivalScreen = purchasesScreen.CanView || stockManagementScreen.CanView;
            CanViewAccountingScreen = accountingScreen.CanView;
            CanViewManualJournalScreen = manualJournalScreen.CanView;
            CanViewReportBuilderScreen = reportBuilderScreen.CanView;
            CanViewBusinessAssistantScreen =
                accountingScreen.CanView ||
                reportBuilderScreen.CanView ||
                purchasesScreen.CanView ||
                stockManagementScreen.CanView;
            CanViewWhatsAppScreen =
                whatsappScreen.CanView ||
                posScreen.CanView ||
                invoiceSearch.CanView ||
                purchasesScreen.CanView ||
                stockManagementScreen.CanView;
            CanViewCarSelectionScreen = carSelectionScreen.CanView;
            CanViewPartSelectionScreen = partSelectionScreen.CanView;
            CanViewBarcodeQrScreen = hasBarcodeQrScreen
                ? barcodeQrScreen.CanView
                : (arScreen?.CanView ?? false) || posScreen.CanView || stockManagementScreen.CanView;
            CanViewArScreen = CanViewBarcodeQrScreen;
            ManagementVm.SetTabPermissions(
                supplierTab.CanView,
                supplierTab.CanEdit,
                supplierTab.CanModify,
                supplierTab.CanDelete,
                currencyTab.CanView,
                transactionTypesTab.CanView);
        }

        private static RoleMenuAccessDto GetMenuAccess(IEnumerable<RoleMenuAccessDto> menuAccessItems, string menuKey)
            => menuAccessItems.FirstOrDefault(i => string.Equals(i.MenuKey, menuKey, StringComparison.OrdinalIgnoreCase))
               ?? new RoleMenuAccessDto { MenuKey = menuKey };

        private void StartArSession()
        {
            StartArSessionAsync().SafeFireAndForget(HandleBackgroundException);
        }

        private async Task StartArSessionAsync()
        {
            try
            {
                var isConnected = await _arDeviceBridge.ConnectAsync();
                if (!isConnected)
                {
                    ArStatusMessage = "Could not connect to AR bridge.";
                    AppNotificationCenter.Instance.Publish("✗ Could not connect to AR bridge.", false);
                    return;
                }

                var selectedLine = SelectedTab?.Items.FirstOrDefault();
                var selectedPart = AvailableParts.FirstOrDefault();
                var request = new ArRenderRequest
                {
                    CarName = SelectedCar?.Name ?? "BMW M3 E92",
                    CarYear = SelectedCar?.Year ?? "2010",
                    EngineType = SelectedCar?.EngineType ?? "S65B40 V8",
                    PartCode = selectedPart?.Code ?? selectedLine?.PartId.ToString() ?? "S65B40",
                    PartDescription = selectedLine?.Description ?? selectedPart?.Description ?? "Complete Engine Assembly",
                    UnitPrice = selectedLine?.UnitPrice ?? selectedPart?.UnitPrice ?? 12000m
                };
                var overlay = await _arRenderingService.RenderOverlayAsync(request);
                await _arDeviceBridge.PushOverlayFrameAsync(overlay);

                IsArSessionActive = true;
                ArOverlayTitle = $"{overlay.CarLabel} → {overlay.PartLabel}";
                ArOverlayDiagnostic = overlay.DiagnosticNote;
                ArReferenceImages = overlay.ReferenceImageUrls.Count == 0
                    ? "No reference images available for this selection."
                    : string.Join(Environment.NewLine, overlay.ReferenceImageUrls);
                _arOverlayPreviewImage = TryCreateBitmapFromOverlay(overlay.ReferenceImageUrls)
                    ?? TryCreateBitmapFromUri("pack://application:,,,/Assets/Logos/bmw.png");
                OnPropertyChanged(nameof(ArPreviewImage));
                ArOverlayLeft = overlay.AnchorX * 540;
                ArOverlayTop = overlay.AnchorY * 320;
                ArStatusMessage = $"AR running. {_arDeviceBridge.LastConnectionDetails}";
                AppNotificationCenter.Instance.Publish("✓ AR session started.", true);
            }
            catch (Exception)
            {
                IsArSessionActive = false;
                ArStatusMessage = "AR startup failed.";
                AppNotificationCenter.Instance.Publish("✗ Failed to start AR session.", false);
            }
        }

        private void StopArSession()
        {
            StopArSessionAsync().SafeFireAndForget(HandleBackgroundException);
        }

        private async Task StopArSessionAsync()
        {
            try
            {
                await _arDeviceBridge.DisconnectAsync();
                IsArSessionActive = false;
                ArStatusMessage = "AR session stopped.";
                ArOverlayDiagnostic = "Disconnected.";
                ArReferenceImages = "No reference images yet.";
                _arOverlayPreviewImage = null;
                OnPropertyChanged(nameof(ArPreviewImage));
                AppNotificationCenter.Instance.Publish("✓ AR session stopped.", true);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Failed to stop AR session.", false);
            }
        }

        private static BitmapImage? TryCreateBitmapFromOverlay(IReadOnlyList<string> imageUrls)
        {
            foreach (var imageUrl in imageUrls)
            {
                var image = TryCreateBitmapFromUri(imageUrl);
                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }

        private static BitmapImage? TryCreateBitmapFromUri(string? uriValue)
        {
            if (string.IsNullOrWhiteSpace(uriValue))
            {
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(uriValue, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }


        private void RefreshInvoiceSearch()
        {
            var version = Interlocked.Increment(ref _invoiceSearchVersion);
            _invoiceSearchCts?.Cancel();
            _invoiceSearchCts?.Dispose();
            _invoiceSearchCts = new CancellationTokenSource();
            RefreshInvoiceSearchAsync(version, _invoiceSearchCts.Token).SafeFireAndForget(HandleBackgroundException);
        }

        private async Task RefreshInvoiceSearchAsync(int version, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(150, cancellationToken);
                if (version != _invoiceSearchVersion || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                IsLoadingInvoiceSearch = true;
                var results = await _salesApi.SearchInvoicesAsync(InvoiceSearchText ?? string.Empty);
                if (version != _invoiceSearchVersion || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (version != _invoiceSearchVersion || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    InvoiceSearchResults.Clear();
                    foreach (var invoice in results)
                    {
                        InvoiceSearchResults.Add(invoice);
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // A newer search superseded this request.
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while searching invoices.", false);
            }
            finally
            {
                if (version == _invoiceSearchVersion)
                {
                    IsLoadingInvoiceSearch = false;
                }
            }
        }

        public async Task OpenInvoiceFromSearchAsync(SalesInvoiceLookupDto? lookup)
        {
            if (lookup == null)
            {
                return;
            }

            var existing = Tabs.FirstOrDefault(t => t.InvoiceId == lookup.InvoiceId);
            if (existing != null)
            {
                SelectedTab = existing;
                ActiveScreen = AppScreen.Pos;
                IsInvoiceSearchOpen = false;
                return;
            }

            try
            {
                IsLoadingInvoiceOpen = true;
                var invoice = await _salesApi.GetInvoiceByIdAsync(lookup.InvoiceId);
                if (invoice == null)
                {
                    AppNotificationCenter.Instance.Publish("✗ Selected invoice was not found.", false);
                    return;
                }

                var tab = new InvoiceTabViewModel(_salesApi, _crudApi);
                tab.LoadFromDatabase(invoice);
                Tabs.Add(tab);
                SelectedTab = tab;
                ActiveScreen = AppScreen.Pos;
                IsInvoiceSearchOpen = false;
                AppNotificationCenter.Instance.Publish($"✓ Loaded invoice {invoice.InvoiceNumber} from database.", true);
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while opening invoice.", false);
            }
            finally
            {
                IsLoadingInvoiceOpen = false;
            }
        }

        private async Task LoadBrandsAsync()
        {
            IsLoadingBrands = true;
            try
            {
                var dtos = await _carCatalogApi.GetCarBrandsAsync();
                var knownOrder = await LoadBrandRegionOrderAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    BrandGroups.Clear();
                    var grouped = dtos
                        .GroupBy(d => d.RegionGroup)
                        .OrderBy(g =>
                        {
                            var key = g.Key?.Trim() ?? string.Empty;
                            var index = knownOrder.FindIndex(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
                            return index >= 0 ? index : int.MaxValue;
                        });

                    foreach (var grp in grouped)
                    {
                        var regionGroup = string.IsNullOrWhiteSpace(grp.Key)
                            ? "OTHER"
                            : grp.Key.ToUpperInvariant();
                        var groupVm = new BrandGroupViewModel { RegionGroup = regionGroup };
                        foreach (var b in grp.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
                        {
                            var bvm = new CarBrandViewModel
                            {
                                Id          = b.Id,
                                Name        = b.Name,
                                Country     = b.Country,
                                RegionGroup = b.RegionGroup
                            };
                            groupVm.Brands.Add(bvm);
                            if (b.HasLogo) LoadLogoAsync(bvm).SafeFireAndForget(HandleBackgroundException);
                        }
                        BrandGroups.Add(groupVm);
                    }
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading brands.", false);
            }
            finally { IsLoadingBrands = false; }
        }

        private async Task<List<string>> LoadBrandRegionOrderAsync()
        {
            try
            {
                var constants = await _crudApi.GetAllAsync<AppConstantDto>("api/appconstants");
                var value = constants
                    .FirstOrDefault(c => string.Equals(c.Key, "BrandRegionOrder", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                var ordered = (value ?? string.Empty)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (ordered.Count > 0)
                {
                    return ordered;
                }
            }
            catch
            {
                // Non-blocking fallback to local defaults.
            }

            return new List<string> { "German", "Japanese", "Korean" };
        }

        private async Task LoadLogoAsync(CarBrandViewModel vm)
        {
            var bmp = await _carCatalogApi.GetCarBrandLogoAsync(vm.Id);
            if (bmp == null) return;
            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.Logo = bmp);
        }

        private void SelectBrand(object? parameter)
        {
            if (parameter is not CarBrandViewModel brand) return;
            if (!CanViewCarSelectionScreen)
            {
                AppNotificationCenter.Instance.Publish("✗ You do not have permission to view car selection.", false);
                return;
            }

            SelectedBrand = brand;
            ActiveScreen  = AppScreen.CarSelection;
            var loadVersion = Interlocked.Increment(ref _carsLoadVersion);
            LoadCarsAsync(brand.Id, loadVersion).SafeFireAndForget(HandleBackgroundException);
        }

        private async Task LoadCarsAsync(int brandId, int loadVersion)
        {
            AvailableCars.Clear();
            IsLoadingCars = true;
            try
            {
                var dtos = await _carCatalogApi.GetCarModelsAsync(brandId);
                if (loadVersion != _carsLoadVersion || SelectedBrand?.Id != brandId)
                {
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (loadVersion != _carsLoadVersion || SelectedBrand?.Id != brandId)
                    {
                        return;
                    }

                    AvailableCars.Clear();
                    foreach (var dto in dtos)
                    {
                        var vm = new CarModelViewModel
                        {
                            Id         = dto.Id,
                            CarBrandId = dto.CarBrandId,
                            CarBrandName = dto.CarBrandName,
                            Name       = string.IsNullOrWhiteSpace(dto.CarBrandName)
                                ? dto.Name
                                : $"{dto.CarBrandName} {dto.Name}",
                            HasImage   = dto.HasImage
                        };
                        AvailableCars.Add(vm);
                        if (dto.HasImage) LoadCarImageAsync(vm).SafeFireAndForget(HandleBackgroundException);
                    }
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading car models.", false);
            }
            finally
            {
                IsLoadingCars = false;
            }
        }

        private async Task LoadCarImageAsync(CarModelViewModel vm)
        {
            var bmp = await _carCatalogApi.GetCarModelImageAsync(vm.Id);
            if (bmp == null) return;
            System.Windows.Application.Current.Dispatcher.Invoke(() => vm.Image = bmp);
        }

        private void SelectCar(object? parameter)
        {
            if (parameter is not CarModelViewModel car) return;
            if (!CanViewPartSelectionScreen)
            {
                AppNotificationCenter.Instance.Publish("✗ You do not have permission to view part selection.", false);
                return;
            }

            SelectedCar  = car;
            ActiveScreen = AppScreen.PartSelection;
            LoadPartsAsync().SafeFireAndForget(HandleBackgroundException);
        }

        private async Task LoadPartsAsync()
        {
            AvailableParts.Clear();
            IsLoadingParts = true;
            try
            {
                var dtos = await _partsApi.GetPartsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableParts.Clear();
                    foreach (var dto in dtos)
                        AvailableParts.Add(new CarPartModel
                        {
                            PartId      = dto.Id,
                            Code        = dto.InternalCode,
                            Description = dto.Name,
                            UnitPrice   = dto.SalePrice
                        });
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading parts.", false);
            }
            finally
            {
                IsLoadingParts = false;
            }
        }

        private async Task LoadStockSnapshotsAsync()
        {
            StockSnapshots.Clear();
            IsLoadingParts = true;

            try
            {
                var parts = await _partsApi.GetPartsAsync();
                var requests = await LoadActivePartRequestsAsync();
                var waitingByPart = SmartPricingCoach.WaitingCustomersByPart(requests);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StockSnapshots.Clear();
                    foreach (var part in parts
                        .OrderBy(part => part.AvailableQuantity > part.MinStock)
                        .ThenBy(part => part.InternalCode))
                    {
                        waitingByPart.TryGetValue(part.Id, out var waitingCustomers);
                        StockSnapshots.Add(StockSnapshotViewModel.FromPart(part, waitingCustomers));
                    }
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading stock.", false);
            }
            finally
            {
                IsLoadingParts = false;
            }
        }

        private async Task<List<PartRequestDto>> LoadActivePartRequestsAsync()
        {
            try
            {
                return await _crudApi.GetAllAsync<PartRequestDto>("api/partrequests?status=Active");
            }
            catch
            {
                return new List<PartRequestDto>();
            }
        }

        private void SelectPart(object? parameter)
        {
            if (parameter is not CarPartModel part || SelectedTab == null) return;
            SelectedTab.Items.Add(new PosItemViewModel
            {
                PartId      = part.PartId,
                Description = part.Description,
                Quantity    = 1,
                UnitPrice   = part.UnitPrice
            });
            ActiveScreen = AppScreen.Pos;
        }

        private void CreateInvoice()
        {
            if (!CanCreateInvoice)
            {
                AppNotificationCenter.Instance.Publish("✗ You do not have permission to create invoices.", false);
                return;
            }

            AddTab();
            ActiveScreen = AppScreen.Pos;
        }

        private void AddTab()
        {
            var tab = new InvoiceTabViewModel(_salesApi, _crudApi);
            Tabs.Add(tab);
            SelectedTab = tab;
            RefreshInvoiceSearch();
        }

        private void CloseTab(InvoiceTabViewModel? tab)
        {
            if (tab == null || Tabs.Count <= 1) return;
            int idx = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            SelectedTab = Tabs[Math.Max(0, idx - 1)];
            RefreshInvoiceSearch();
        }



       

        public void OpenInvoiceFromSearch(InvoiceTabViewModel? tab)
        {
            if (tab == null)
            {
                return;
            }

            SelectedTab = tab;
            ActiveScreen = AppScreen.Pos;
            tab.MarkOpenedFromSearch();
            IsInvoiceSearchOpen = false;
            AppNotificationCenter.Instance.Publish($"✓ Opened {tab.Header} for editing.", true);
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private void HandleBackgroundException(Exception ex)
            => AppNotificationCenter.Instance.Publish($"✗ Background task failed: {ex.Message}", false);
    }
}
