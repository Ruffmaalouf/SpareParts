using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class CarModelViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int CarBrandId { get; set; }
        public string CarBrandName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Year { get; set; }
        public string? EngineType { get; set; }
        public decimal? BasePrice { get; set; }
        public bool HasImage { get; set; }

        private BitmapImage? _image;
        public BitmapImage? Image
        {
            get => _image;
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
