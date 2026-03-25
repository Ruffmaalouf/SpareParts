using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class WarehouseManagementViewModel
    {
        public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
        public WarehouseDto? SelectedWarehouse { get; set; }

        public string NewWarehouseName { get; set; } = string.Empty;
        public string NewWarehouseAddress { get; set; } = string.Empty;
        public bool NewWarehouseIsMain { get; set; }

        public void PopulateForm(WarehouseDto warehouse)
        {
            NewWarehouseName = warehouse.Name;
            NewWarehouseAddress = warehouse.Address ?? string.Empty;
            NewWarehouseIsMain = warehouse.IsMain;
        }

        public void ClearForm()
        {
            NewWarehouseName = string.Empty;
            NewWarehouseAddress = string.Empty;
            NewWarehouseIsMain = false;
            SelectedWarehouse = null;
        }
    }
}
