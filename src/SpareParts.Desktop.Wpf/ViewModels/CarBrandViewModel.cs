using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class CarBrandViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public bool HasLogo { get; set; }

        private BitmapImage? _logo;
        public BitmapImage? Logo
        {
            get => _logo;
            set { _logo = value; OnPropertyChanged(nameof(Logo)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
