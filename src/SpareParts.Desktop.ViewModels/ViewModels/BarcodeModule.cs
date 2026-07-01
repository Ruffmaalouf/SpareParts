namespace SpareParts.Desktop.Wpf.ViewModels;

public sealed class BarcodeModule
{
    public double Width { get; init; }
    public bool IsBar { get; init; }
    public string Fill => IsBar ? "#111111" : "Transparent";
}
