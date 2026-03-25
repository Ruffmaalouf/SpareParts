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
        public string NewCarModelYear { get; set; } = string.Empty;
        public string NewCarModelEngine { get; set; } = string.Empty;
        public decimal NewCarModelBasePrice { get; set; }
        public int NewCarModelBrandId { get; set; }
        public CarModelDto? SelectedCarModel { get; set; }

        public void PopulateForm(CarModelDto m)
        {
            NewCarModelBrandId = m.CarBrandId;
            NewCarModelName = m.Name;
            NewCarModelYear = m.Year;
            NewCarModelEngine = m.EngineType;
            NewCarModelBasePrice = m.BasePrice;
        }

        public void ClearCarBrandForm()
        {
            NewCarBrandName = string.Empty;
            NewCarBrandCountry = string.Empty;
            NewCarBrandRegionGroup = string.Empty;
            NewCarBrandSortOrder = 0;
        }

        public void ClearForm()
        {
            NewCarModelName = NewCarModelYear = NewCarModelEngine = string.Empty;
            NewCarModelBasePrice = 0;
            NewCarModelBrandId = 0;
            SelectedCarModel = null;
        }
    }
}
