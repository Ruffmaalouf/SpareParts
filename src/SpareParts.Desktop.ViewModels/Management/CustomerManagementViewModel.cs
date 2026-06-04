using SpareParts.Domain.BusinessPartners;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class CustomerManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private CustomerDto? _selectedCustomer;
        private string _newCustomerName = string.Empty;
        private string _newCustomerPhone = string.Empty;
        private string _newCustomerEmail = string.Empty;
        private string _newCustomerAddress = string.Empty;
        private string _newCustomerTax = string.Empty;
        private decimal _newCustomerBalance;

        public CustomerManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.Customers"));
        }

        public ObservableCollection<CustomerDto> Customers { get; } = new();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }

        public CustomerDto? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (!SetProperty(ref _selectedCustomer, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

        public string NewCustomerName
        {
            get => _newCustomerName;
            set => SetProperty(ref _newCustomerName, value);
        }

        public string NewCustomerPhone
        {
            get => _newCustomerPhone;
            set => SetProperty(ref _newCustomerPhone, value);
        }

        public string NewCustomerEmail
        {
            get => _newCustomerEmail;
            set => SetProperty(ref _newCustomerEmail, value);
        }

        public string NewCustomerAddress
        {
            get => _newCustomerAddress;
            set => SetProperty(ref _newCustomerAddress, value);
        }

        public string NewCustomerTax
        {
            get => _newCustomerTax;
            set => SetProperty(ref _newCustomerTax, value);
        }

        public decimal NewCustomerBalance
        {
            get => _newCustomerBalance;
            set => SetProperty(ref _newCustomerBalance, value);
        }

        public void PopulateForm(CustomerDto c)
        {
            NewCustomerName = c.Name;
            NewCustomerPhone = c.Phone ?? string.Empty;
            NewCustomerEmail = c.Email ?? string.Empty;
            NewCustomerAddress = c.Address ?? string.Empty;
            NewCustomerTax = c.TaxNumber ?? string.Empty;
            NewCustomerBalance = c.OpeningBalance;
        }

        public void ClearForm()
        {
            NewCustomerName = NewCustomerPhone = NewCustomerEmail = NewCustomerAddress = NewCustomerTax = string.Empty;
            NewCustomerBalance = 0;
            SelectedCustomer = null;
        }

        public void StartNew() => ClearForm();

        private async Task SaveAsync()
        {
            var result = await _ctx.Coordinator.SaveCustomerAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }

        private async Task DeleteAsync()
        {
            var result = await _ctx.Coordinator.DeleteCustomerAsync(SelectedCustomer);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            ClearForm();
        }
    }
}
