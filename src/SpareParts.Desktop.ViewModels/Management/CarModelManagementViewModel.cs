using SpareParts.Domain.Cars;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class CarModelManagementViewModel
    {
        public ObservableCollection<CarModelDto> CarModels { get; } = new();
        public ObservableCollection<CarBrandDto> CarBrands { get; } = new();

        public string NewCarBrandName { get; set; } = string.Empty;
        public string NewCarBrandCountry { get; set; } = string.Empty;
        public string NewCarBrandRegionGroup { get; set; } = string.Empty;
        public int NewCarBrandSortOrder { get; set; }

        public string NewCarModelName { get; set; } = string.Empty;
        public string NewCarModelBodyType { get; set; } = string.Empty;
        public int NewCarModelBrandId { get; set; }
        public CarBrandDto? SelectedCarBrand { get; set; }
        public CarModelDto? SelectedCarModel { get; set; }

        public void PopulateCarBrandForm(CarBrandDto brand)
        {
            NewCarBrandName = brand.Name;
            NewCarBrandCountry = brand.Country;
            NewCarBrandRegionGroup = brand.RegionGroup;
            NewCarBrandSortOrder = brand.SortOrder;
        }

        public void PopulateForm(CarModelDto m)
        {
            NewCarModelBrandId = m.CarBrandId;
            NewCarModelName = m.Name;
            NewCarModelBodyType = m.BodyType;
        }

        public void ClearCarBrandForm()
        {
            NewCarBrandName = string.Empty;
            NewCarBrandCountry = string.Empty;
            NewCarBrandRegionGroup = string.Empty;
            NewCarBrandSortOrder = 0;
            SelectedCarBrand = null;
        }

        public void ClearForm()
        {
            NewCarModelName = string.Empty;
            NewCarModelBodyType = string.Empty;
            NewCarModelBrandId = 0;
            SelectedCarModel = null;
        }
    }
}
