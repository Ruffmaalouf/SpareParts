using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.ViewModels;

public sealed class BarcodeLabelItem
{
    public int PartId { get; init; }
    public string PartCode { get; init; } = string.Empty;
    public string PartName { get; init; } = string.Empty;
    public string BarcodeText { get; init; } = string.Empty;
    public string QrPayload { get; init; } = string.Empty;
    public string PriceText { get; init; } = string.Empty;
    public string StockText { get; init; } = string.Empty;
    public BitmapImage? QrImage { get; init; }
    public ObservableCollection<BarcodeModule> BarcodeModules { get; init; } = new();
}
