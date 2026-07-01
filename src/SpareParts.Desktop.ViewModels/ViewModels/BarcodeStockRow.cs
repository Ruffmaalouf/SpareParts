using SpareParts.Domain.Inventory;

namespace SpareParts.Desktop.Wpf.ViewModels;

public sealed class BarcodeStockRow
{
    public BarcodeStockRow(PartStockDto dto)
    {
        WarehouseName = dto.WarehouseName;
        Quantity = dto.Quantity;
        ReservedQuantity = dto.ReservedQuantity;
        AvailableQuantity = dto.AvailableQuantity;
    }

    public string WarehouseName { get; }
    public int Quantity { get; }
    public int ReservedQuantity { get; }
    public int AvailableQuantity { get; }
    public string Status => AvailableQuantity <= 0 ? "Out" : "Available";
}
