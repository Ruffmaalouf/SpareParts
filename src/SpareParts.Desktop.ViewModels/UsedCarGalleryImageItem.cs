using System;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public sealed class UsedCarGalleryImageItem
    {
        public int Id { get; init; }
        public BitmapImage? Preview { get; init; }
        public DateTime CreatedAt { get; init; }
        public string MimeType { get; init; } = "image/png";
    }
}
