using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Cars;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class CarModelManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private string _newCarBrandName = string.Empty;
        private string _newCarBrandCountry = string.Empty;
        private string _newCarBrandRegionGroup = string.Empty;
        private int _newCarBrandSortOrder;
        private string _newCarModelName = string.Empty;
        private string _newCarModelBodyType = string.Empty;
        private int _newCarModelBrandId;
        private CarBrandDto? _selectedCarBrand;
        private CarModelDto? _selectedCarModel;

        public ObservableCollection<CarModelDto> CarModels { get; } = new();
        public ObservableCollection<CarBrandDto> CarBrands { get; } = new();
        public ICommand SaveCarBrandCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCarBrandCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCarBrandCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportCarBrandsFromExcelCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand SaveCarModelCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCarModelCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCarModelCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportCarModelsFromExcelCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });

        public void Configure(
            ManagementCoordinator coordinator,
            Func<Task> refreshAsync,
            Action<string, bool> setStatus,
            ICommand? importTableCommand = null)
        {
            _coordinator = coordinator;
            _refreshAsync = refreshAsync;
            _setStatus = setStatus;
            SaveCarBrandCommand = new RelayCommand(_ => _ = SaveCarBrandAsync());
            DeleteCarBrandCommand = new RelayCommand(_ => _ = DeleteCarBrandAsync());
            StartNewCarBrandCommand = new RelayCommand(_ => StartNewCarBrand());
            ImportCarBrandsFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.CarBrands"));
            SaveCarModelCommand = new RelayCommand(_ => _ = SaveCarModelAsync());
            DeleteCarModelCommand = new RelayCommand(_ => _ = DeleteCarModelAsync());
            StartNewCarModelCommand = new RelayCommand(_ => StartNewCarModel());
            ImportCarModelsFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.CarModels"));
            RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
        }

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
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveCarBrandAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearCarBrandForm();
        }

        private async Task DeleteCarBrandAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeleteCarBrandAsync(SelectedCarBrand);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearCarBrandForm();
        }

        private async Task SaveCarModelAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveCarModelAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearForm();
        }

        private async Task DeleteCarModelAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeleteCarModelAsync(SelectedCarModel);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearForm();
        }
    }
}
