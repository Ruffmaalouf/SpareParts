using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Cars;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class CarModelManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private string _newCarBrandName = string.Empty;
        private string _newCarBrandCountry = string.Empty;
        private string _newCarBrandRegionGroup = string.Empty;
        private int _newCarBrandSortOrder;
        private string _newCarModelName = string.Empty;
        private string _newCarModelBodyType = string.Empty;
        private int _newCarModelBrandId;
        private CarBrandDto? _selectedCarBrand;
        private CarModelDto? _selectedCarModel;

        public CarModelManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            SaveCarBrandCommand = new RelayCommand(_ => _ = SaveCarBrandAsync());
            DeleteCarBrandCommand = new RelayCommand(_ => _ = DeleteCarBrandAsync());
            StartNewCarBrandCommand = new RelayCommand(_ => StartNewCarBrand());
            ImportCarBrandsFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.CarBrands"));
            SaveCarModelCommand = new RelayCommand(_ => _ = SaveCarModelAsync());
            DeleteCarModelCommand = new RelayCommand(_ => _ = DeleteCarModelAsync());
            StartNewCarModelCommand = new RelayCommand(_ => StartNewCarModel());
            ImportCarModelsFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.CarModels"));
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
        }

        public ObservableCollection<CarModelDto> CarModels { get; } = new();
        public ObservableCollection<CarBrandDto> CarBrands { get; } = new();
        public ICommand SaveCarBrandCommand { get; }
        public ICommand DeleteCarBrandCommand { get; }
        public ICommand StartNewCarBrandCommand { get; }
        public ICommand ImportCarBrandsFromExcelCommand { get; }
        public ICommand SaveCarModelCommand { get; }
        public ICommand DeleteCarModelCommand { get; }
        public ICommand StartNewCarModelCommand { get; }
        public ICommand ImportCarModelsFromExcelCommand { get; }
        public ICommand RefreshCommand { get; }

        public string NewCarBrandName
        {
            get => _newCarBrandName;
            set => SetProperty(ref _newCarBrandName, value);
        }

        public string NewCarBrandCountry
        {
            get => _newCarBrandCountry;
            set => SetProperty(ref _newCarBrandCountry, value);
        }

        public string NewCarBrandRegionGroup
        {
            get => _newCarBrandRegionGroup;
            set => SetProperty(ref _newCarBrandRegionGroup, value);
        }

        public int NewCarBrandSortOrder
        {
            get => _newCarBrandSortOrder;
            set => SetProperty(ref _newCarBrandSortOrder, value);
        }

        public string NewCarModelName
        {
            get => _newCarModelName;
            set => SetProperty(ref _newCarModelName, value);
        }

        public string NewCarModelBodyType
        {
            get => _newCarModelBodyType;
            set => SetProperty(ref _newCarModelBodyType, value);
        }

        public int NewCarModelBrandId
        {
            get => _newCarModelBrandId;
            set => SetProperty(ref _newCarModelBrandId, value);
        }

        public CarBrandDto? SelectedCarBrand
        {
            get => _selectedCarBrand;
            set
            {
                if (!SetProperty(ref _selectedCarBrand, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateCarBrandForm(value);
                }
            }
        }

        public CarModelDto? SelectedCarModel
        {
            get => _selectedCarModel;
            set
            {
                if (!SetProperty(ref _selectedCarModel, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

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

        public void StartNewCarBrand() => ClearCarBrandForm();
        public void StartNewCarModel() => ClearForm();

        private async Task SaveCarBrandAsync()
        {
            var result = await _ctx.Coordinator.SaveCarBrandAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearCarBrandForm();
        }

        private async Task DeleteCarBrandAsync()
        {
            var result = await _ctx.Coordinator.DeleteCarBrandAsync(SelectedCarBrand);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearCarBrandForm();
        }

        private async Task SaveCarModelAsync()
        {
            var result = await _ctx.Coordinator.SaveCarModelAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }

        private async Task DeleteCarModelAsync()
        {
            var result = await _ctx.Coordinator.DeleteCarModelAsync(SelectedCarModel);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }
    }
}
