using SpareParts.Domain.Inventory;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class BrandManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private string _newBrandName = string.Empty;
        private bool _newBrandIsActive = true;
        private BrandDto? _selectedBrand;

        public ObservableCollection<BrandDto> Brands { get; } = new();
        public ICommand SaveCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportFromExcelCommand { get; private set; } = new RelayCommand(_ => { });

        public void Configure(
            ManagementCoordinator coordinator,
            Func<Task> refreshAsync,
            Action<string, bool> setStatus,
            ICommand? importTableCommand = null)
        {
            _coordinator = coordinator;
            _refreshAsync = refreshAsync;
            _setStatus = setStatus;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.Brands"));
        }

        public string NewBrandName
        {
            get => _newBrandName;
            set => SetProperty(ref _newBrandName, value);
        }

        public bool NewBrandIsActive
        {
            get => _newBrandIsActive;
            set => SetProperty(ref _newBrandIsActive, value);
        }

        public BrandDto? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (!SetProperty(ref _selectedBrand, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

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

        public void StartNew() => ClearForm();

        private async Task SaveAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveBrandAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            ClearForm();
        }

        private async Task DeleteAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeleteBrandAsync(SelectedBrand);
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
