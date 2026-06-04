using SpareParts.Domain.BusinessPartners;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class SupplierManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private readonly SupplierPermissionState _permissions = new();
        private SupplierDto? _selectedSupplier;
        private string _newSupplierName = string.Empty;
        private string _newSupplierPhone = string.Empty;
        private string _newSupplierEmail = string.Empty;
        private string _newSupplierAddress = string.Empty;
        private string _newSupplierTax = string.Empty;
        private decimal _newSupplierBalance;

        public SupplierManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            _permissions.PropertyChanged += PermissionsOnPropertyChanged;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.Suppliers"));
        }

        public ObservableCollection<SupplierDto> Suppliers { get; } = new();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }

        public bool CanViewSupplierTab => _permissions.CanViewSupplierTab;
        public bool CanEditSupplier => _permissions.CanEditSupplier;
        public bool CanModifySupplier => _permissions.CanModifySupplier;
        public bool CanDeleteSupplier => _permissions.CanDeleteSupplier;
        public bool CanSaveSupplier => _permissions.CanSaveSupplier;

        public void SetPermissions(bool canViewSupplierTab, bool canEditSupplier, bool canModifySupplier, bool canDeleteSupplier)
            => _permissions.Set(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);

        public SupplierDto? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (!SetProperty(ref _selectedSupplier, value))
                {
                    return;
                }

                _permissions.IsEditingSupplier = value != null;
                OnPropertyChanged(nameof(CanSaveSupplier));

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

        public string NewSupplierName
        {
            get => _newSupplierName;
            set => SetProperty(ref _newSupplierName, value);
        }

        public string NewSupplierPhone
        {
            get => _newSupplierPhone;
            set => SetProperty(ref _newSupplierPhone, value);
        }

        public string NewSupplierEmail
        {
            get => _newSupplierEmail;
            set => SetProperty(ref _newSupplierEmail, value);
        }

        public string NewSupplierAddress
        {
            get => _newSupplierAddress;
            set => SetProperty(ref _newSupplierAddress, value);
        }

        public string NewSupplierTax
        {
            get => _newSupplierTax;
            set => SetProperty(ref _newSupplierTax, value);
        }

        public decimal NewSupplierBalance
        {
            get => _newSupplierBalance;
            set => SetProperty(ref _newSupplierBalance, value);
        }

        public void PopulateForm(SupplierDto s)
        {
            NewSupplierName = s.Name;
            NewSupplierPhone = s.Phone ?? string.Empty;
            NewSupplierEmail = s.Email ?? string.Empty;
            NewSupplierAddress = s.Address ?? string.Empty;
            NewSupplierTax = s.TaxNumber ?? string.Empty;
            NewSupplierBalance = s.OpeningBalance;
        }

        public void ClearForm()
        {
            NewSupplierName = NewSupplierPhone = NewSupplierEmail = NewSupplierAddress = NewSupplierTax = string.Empty;
            NewSupplierBalance = 0;
            SelectedSupplier = null;
        }

        public void StartNew()
        {
            ClearForm();
            _permissions.IsEditingSupplier = false;
            OnPropertyChanged(nameof(CanSaveSupplier));
        }

        private async Task SaveAsync()
        {
            if (!CanViewSupplierTab)
            {
                _ctx.SetStatus("✗ You do not have permission to view the supplier tab.", false);
                return;
            }

            var isEditing = SelectedSupplier != null;
            if (!isEditing && !CanEditSupplier)
            {
                _ctx.SetStatus("✗ You do not have permission to create suppliers.", false);
                return;
            }

            if (isEditing && !CanModifySupplier)
            {
                _ctx.SetStatus("✗ You do not have permission to modify suppliers.", false);
                return;
            }

            var result = await _ctx.Coordinator.SaveSupplierAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            if (!CanViewSupplierTab)
            {
                _ctx.SetStatus("✗ You do not have permission to view the supplier tab.", false);
                return;
            }

            if (!CanDeleteSupplier)
            {
                _ctx.SetStatus("✗ You do not have permission to delete suppliers.", false);
                return;
            }

            var result = await _ctx.Coordinator.DeleteSupplierAsync(SelectedSupplier);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }

        private void PermissionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.PropertyName))
            {
                return;
            }

            OnPropertyChanged(e.PropertyName);
        }
    }
}
