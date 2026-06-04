using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class WarehouseManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private WarehouseDto? _selectedWarehouse;
        private string _newWarehouseName = string.Empty;
        private string _newWarehouseBarcode = string.Empty;
        private string _newWarehouseAddress = string.Empty;
        private bool _newWarehouseIsMain;

        public WarehouseManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.Warehouses"));
        }

        public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }

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
            var result = await _ctx.Coordinator.SaveWarehouseAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }

        private async Task DeleteAsync()
        {
            var result = await _ctx.Coordinator.DeleteWarehouseAsync(SelectedWarehouse);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }
    }
}
