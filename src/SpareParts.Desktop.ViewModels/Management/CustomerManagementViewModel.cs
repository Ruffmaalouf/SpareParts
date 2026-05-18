using SpareParts.Domain.BusinessPartners;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class CustomerManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private CustomerDto? _selectedCustomer;
        private string _newCustomerName = string.Empty;
        private string _newCustomerPhone = string.Empty;
        private string _newCustomerEmail = string.Empty;
        private string _newCustomerAddress = string.Empty;
        private string _newCustomerTax = string.Empty;
        private decimal _newCustomerBalance;

        public ObservableCollection<CustomerDto> Customers { get; } = new();
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
            ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.Customers"));
        }

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
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveCustomerAsync(this);
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

            var result = await _coordinator.DeleteCustomerAsync(SelectedCustomer);
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
