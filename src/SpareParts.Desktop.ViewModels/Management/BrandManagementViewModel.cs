using SpareParts.Domain.Inventory;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class BrandManagementViewModel
    {
        public ObservableCollection<BrandDto> Brands { get; } = new();
        public string NewBrandName { get; set; } = string.Empty;
        public bool NewBrandIsActive { get; set; } = true;
        public BrandDto? SelectedBrand { get; set; }

        public void PopulateForm(BrandDto b)
        {
            NewBrandName = b.Name;
            NewBrandIsActive = b.IsActive;
        }

        public void ClearForm()
        {
            NewBrandName = string.Empty;
            NewBrandIsActive = true;
            SelectedBrand = null;
        }
    }
}
