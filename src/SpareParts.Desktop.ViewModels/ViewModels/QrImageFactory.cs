using QRCoder;
using System.IO;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.ViewModels;

internal static class QrImageFactory
{
    public static BitmapImage Create(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(8, System.Drawing.Color.FromArgb(17, 17, 17), System.Drawing.Color.White);

        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
