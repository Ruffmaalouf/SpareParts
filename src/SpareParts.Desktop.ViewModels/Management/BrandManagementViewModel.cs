using SpareParts.Domain.Inventory;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class BrandManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private string _newBrandName = string.Empty;
        private bool _newBrandIsActive = true;
        private BrandDto? _selectedBrand;

        public BrandManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.Brands"));
        }

        public ObservableCollection<BrandDto> Brands { get; } = new();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }

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
            var result = await _ctx.Coordinator.SaveBrandAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }

        private async Task DeleteAsync()
        {
            var result = await _ctx.Coordinator.DeleteBrandAsync(SelectedBrand);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }
    }
}
