using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public class RolesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RoleManagementDto> Roles { get; } = new();

        // ── Form fields ───────────────────────────────────────────────────────
        private RoleManagementDto? _selectedRole;
        public RoleManagementDto? SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged(nameof(SelectedRole));
                OnPropertyChanged(nameof(IsEditing));
                if (value != null) PopulateForm(value);
                else ClearForm();
            }
        }

        public bool IsEditing => _selectedRole != null;

        private string _formName        = string.Empty;
        private string _formDescription = string.Empty;
        private string _formBadgeColor     = "#22FFFFFF";
        private string _formBadgeTextColor = "#FFFFFF";
        private bool   _formIsActive    = true;
        private int _permissionsRoleId;
        private bool _canViewInvoiceSearch;
        private bool _canViewManagementScreen;
        private bool _canViewSupplierTab;
        private bool _canEditSupplier;
        private bool _canModifySupplier;
        private bool _canDeleteSupplier;

        public string FormName           { get => _formName;           set { _formName           = value; OnPropertyChanged(nameof(FormName)); } }
        public string FormDescription    { get => _formDescription;    set { _formDescription    = value; OnPropertyChanged(nameof(FormDescription)); } }
        public string FormBadgeColor     { get => _formBadgeColor;     set { _formBadgeColor     = value; OnPropertyChanged(nameof(FormBadgeColor)); } }
        public string FormBadgeTextColor { get => _formBadgeTextColor; set { _formBadgeTextColor = value; OnPropertyChanged(nameof(FormBadgeTextColor)); } }
        public bool   FormIsActive       { get => _formIsActive;       set { _formIsActive       = value; OnPropertyChanged(nameof(FormIsActive)); } }
        public bool CanViewInvoiceSearch { get => _canViewInvoiceSearch; set { _canViewInvoiceSearch = value; OnPropertyChanged(nameof(CanViewInvoiceSearch)); } }
        public bool CanViewManagementScreen { get => _canViewManagementScreen; set { _canViewManagementScreen = value; OnPropertyChanged(nameof(CanViewManagementScreen)); } }
        public bool CanViewSupplierTab { get => _canViewSupplierTab; set { _canViewSupplierTab = value; OnPropertyChanged(nameof(CanViewSupplierTab)); } }
        public bool CanEditSupplier { get => _canEditSupplier; set { _canEditSupplier = value; OnPropertyChanged(nameof(CanEditSupplier)); } }
        public bool CanModifySupplier { get => _canModifySupplier; set { _canModifySupplier = value; OnPropertyChanged(nameof(CanModifySupplier)); } }
        public bool CanDeleteSupplier { get => _canDeleteSupplier; set { _canDeleteSupplier = value; OnPropertyChanged(nameof(CanDeleteSupplier)); } }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private Brush _statusBrush = Brushes.Gray;
        public Brush StatusBrush
        {
            get => _statusBrush;
            set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        // ── Dependencies / Commands ──────────────────────────────────────────
        private readonly IRoleApiClient _rolesApi;

        public ICommand LoadCommand   { get; }
        public ICommand NewCommand    { get; }
        public ICommand SaveCommand   { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SavePermissionsCommand { get; }

        public RolesViewModel(IRoleApiClient? rolesApi = null)
        {
            _rolesApi = rolesApi ?? new RolesApiClient();

            LoadCommand   = new RelayCommand(_ => _ = LoadAsync());
            NewCommand    = new RelayCommand(_ => { SelectedRole = null; ClearForm(); });
            SaveCommand   = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(r => _ = DeleteAsync(r as RoleManagementDto));
            SavePermissionsCommand = new RelayCommand(_ => _ = SavePermissionsAsync());
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            IsBusy = true;
            SetStatus(string.Empty, true);
            try
            {
                var list = await _rolesApi.GetRolesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Roles.Clear();
                    foreach (var r in list)
                        Roles.Add(new RoleManagementDto
                        {
                            Id             = r.Id,
                            Name           = r.Name,
                            Description    = r.Description,
                            BadgeColor     = r.BadgeColor,
                            BadgeTextColor = r.BadgeTextColor,
                            IsSystem       = r.IsSystem,
                            IsActive       = r.IsActive
                        });
                });
                SetStatus($"✓ {Roles.Count} role(s) loaded.", true);
                if (SelectedRole == null)
                {
                    ClearPermissions();
                }
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while loading roles.", false); }
            finally { IsBusy = false; }
        }

        // ── Save (create or update) ───────────────────────────────────────────
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FormName))
            { SetStatus("✗ Role name is required.", false); return; }

            IsBusy = true;
            try
            {
                if (_selectedRole == null)
                {
                    // Create
                    var createdRole = await _rolesApi.CreateRoleAsync(new CreateRoleRequest
                    {
                        Name           = FormName.Trim(),
                        Description    = FormDescription.Trim(),
                        BadgeColor     = FormBadgeColor.Trim(),
                        BadgeTextColor = FormBadgeTextColor.Trim()
                    });
                    await _rolesApi.UpdateRolePermissionsAsync(createdRole.Id, BuildPermissionsRequest());
                    SetStatus($"✓ Role '{FormName}' created.", true);
                }
                else
                {
                    // Update
                    await _rolesApi.UpdateRoleAsync(_selectedRole.Id, new UpdateRoleRequest
                    {
                        Description    = FormDescription.Trim(),
                        BadgeColor     = FormBadgeColor.Trim(),
                        BadgeTextColor = FormBadgeTextColor.Trim(),
                        IsActive       = FormIsActive
                    });
                    await _rolesApi.UpdateRolePermissionsAsync(_selectedRole.Id, BuildPermissionsRequest());
                    SetStatus($"✓ Role '{_selectedRole.Name}' updated.", true);
                }

                ClearForm();
                await LoadAsync();
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while saving role.", false); }
            finally { IsBusy = false; }
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private async Task DeleteAsync(RoleManagementDto? role)
        {
            if (role == null) return;
            if (role.IsSystem)
            { SetStatus("✗ Built-in system roles cannot be deleted.", false); return; }

            IsBusy = true;
            try
            {
                await _rolesApi.DeleteRoleAsync(role.Id);
                SetStatus($"✓ Role '{role.Name}' deactivated.", true);
                await LoadAsync();
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while deleting role.", false); }
            finally { IsBusy = false; }
        }

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.MediumSeaGreen : Brushes.IndianRed;
            AppNotificationCenter.Instance.Publish(message, isSuccess);
        }

        private void PopulateForm(RoleManagementDto r)
        {
            _permissionsRoleId = r.Id;
            FormName           = r.Name;
            FormDescription    = r.Description ?? string.Empty;
            FormBadgeColor     = r.BadgeColor;
            FormBadgeTextColor = r.BadgeTextColor;
            FormIsActive       = r.IsActive;
            _ = LoadPermissionsAsync(r.Id);
        }

        private void ClearForm()
        {
            FormName           = string.Empty;
            FormDescription    = string.Empty;
            FormBadgeColor     = "#22FFFFFF";
            FormBadgeTextColor = "#FFFFFF";
            FormIsActive       = true;
            _permissionsRoleId = 0;
            ClearPermissions();
        }

        private async Task LoadPermissionsAsync(int roleId)
        {
            try
            {
                var permissions = await _rolesApi.GetRolePermissionsAsync(roleId);
                _permissionsRoleId = roleId;
                CanViewInvoiceSearch = permissions.CanViewInvoiceSearch;
                CanViewManagementScreen = permissions.CanViewManagementScreen;
                CanViewSupplierTab = permissions.CanViewSupplierTab;
                CanEditSupplier = permissions.CanEditSupplier;
                CanModifySupplier = permissions.CanModifySupplier;
                CanDeleteSupplier = permissions.CanDeleteSupplier;
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while loading role permissions.", false); }
        }

        private async Task SavePermissionsAsync()
        {
            if (_permissionsRoleId <= 0)
            {
                SetStatus("✗ Select a role first before saving permissions.", false);
                return;
            }

            try
            {
                await _rolesApi.UpdateRolePermissionsAsync(_permissionsRoleId, BuildPermissionsRequest());
                SetStatus("✓ Permissions updated.", true);
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while saving permissions.", false); }
        }

        private UpdateRoleScreenPermissionsRequest BuildPermissionsRequest() => new()
        {
            CanViewInvoiceSearch = CanViewInvoiceSearch,
            CanViewManagementScreen = CanViewManagementScreen,
            CanViewSupplierTab = CanViewSupplierTab,
            CanEditSupplier = CanEditSupplier,
            CanModifySupplier = CanModifySupplier,
            CanDeleteSupplier = CanDeleteSupplier
        };

        private void ClearPermissions()
        {
            CanViewInvoiceSearch = false;
            CanViewManagementScreen = false;
            CanViewSupplierTab = false;
            CanEditSupplier = false;
            CanModifySupplier = false;
            CanDeleteSupplier = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
