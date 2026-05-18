using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class WarehouseManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private WarehouseDto? _selectedWarehouse;
        private string _newWarehouseName = string.Empty;
        private string _newWarehouseBarcode = string.Empty;
        private string _newWarehouseAddress = string.Empty;
        private bool _newWarehouseIsMain;

        public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
        public ICommand SaveCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportFromExcelCommand { get; private set; } = new RelayCommand(_ => { });

        public void Configure(
            ManagementCoordinator coordinator,
            Func<Task> refreshAsync,
            Action<string, bool> setStatus,
            ICommand? importTableCommand = null)
        {
            _coordinator = coordinator;
            _refreshAsync = refreshAsync;
            _setStatus = setStatus;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.Warehouses"));
        }

        public WarehouseDto? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (!SetProperty(ref _selectedWarehouse, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

        public string NewWarehouseName
        {
            get => _newWarehouseName;
            set => SetProperty(ref _newWarehouseName, value);
        }

        public string NewWarehouseBarcode
        {
            get => _newWarehouseBarcode;
            set => SetProperty(ref _newWarehouseBarcode, value);
        }

        public string NewWarehouseAddress
        {
            get => _newWarehouseAddress;
            set => SetProperty(ref _newWarehouseAddress, value);
        }

        public bool NewWarehouseIsMain
        {
            get => _newWarehouseIsMain;
            set => SetProperty(ref _newWarehouseIsMain, value);
        }

        public void PopulateForm(WarehouseDto warehouse)
        {
            NewWarehouseName = warehouse.Name;
            NewWarehouseBarcode = warehouse.Barcode ?? string.Empty;
            NewWarehouseAddress = warehouse.Address ?? string.Empty;
            NewWarehouseIsMain = warehouse.IsMain;
        }

        public void ClearForm()
        {
            NewWarehouseName = string.Empty;
            NewWarehouseBarcode = string.Empty;
            NewWarehouseAddress = string.Empty;
            NewWarehouseIsMain = false;
            SelectedWarehouse = null;
        }

        public void StartNew() => ClearForm();

        private async Task SaveAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveWarehouseAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearForm();
        }

        private async Task DeleteAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeleteWarehouseAsync(SelectedWarehouse);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearForm();
        }
    }
}
