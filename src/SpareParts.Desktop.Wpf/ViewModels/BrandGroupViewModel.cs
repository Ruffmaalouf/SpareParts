 
using System.Collections.ObjectModel; 

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class BrandGroupViewModel
    {
        public string RegionGroup { get; set; } = string.Empty;
        public ObservableCollection<CarBrandViewModel> Brands { get; } = new();
    }
  
}
