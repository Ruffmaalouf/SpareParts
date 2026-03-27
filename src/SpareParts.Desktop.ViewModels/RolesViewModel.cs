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
        public ObservableCollection<RoleMenuAccessDto> MenuAccessItems { get; } = new();

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

        private string _formName = string.Empty;
        private string _formDescription = string.Empty;
        private string _formBadgeColor = "#22FFFFFF";
        private string _formBadgeTextColor = "#FFFFFF";
        private bool _formIsActive = true;
        private string _accessPopupTitle = "Role Access";
        private bool _isAccessPopupOpen;
        private int? _accessRoleId;

        public string FormName { get => _formName; set { _formName = value; OnPropertyChanged(nameof(FormName)); } }
        public string FormDescription { get => _formDescription; set { _formDescription = value; OnPropertyChanged(nameof(FormDescription)); } }
        public string FormBadgeColor { get => _formBadgeColor; set { _formBadgeColor = value; OnPropertyChanged(nameof(FormBadgeColor)); } }
        public string FormBadgeTextColor { get => _formBadgeTextColor; set { _formBadgeTextColor = value; OnPropertyChanged(nameof(FormBadgeTextColor)); } }
        public bool FormIsActive { get => _formIsActive; set { _formIsActive = value; OnPropertyChanged(nameof(FormIsActive)); } }
        public string AccessPopupTitle { get => _accessPopupTitle; set { _accessPopupTitle = value; OnPropertyChanged(nameof(AccessPopupTitle)); } }
        public bool IsAccessPopupOpen { get => _isAccessPopupOpen; set { _isAccessPopupOpen = value; OnPropertyChanged(nameof(IsAccessPopupOpen)); } }

        private string _status = string.Empty;
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        private Brush _statusBrush = Brushes.Gray;
        public Brush StatusBrush { get => _statusBrush; set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); } }

        private readonly IRoleApiClient _rolesApi;

        public ICommand LoadCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ViewAccessCommand { get; }
        public ICommand SaveAccessCommand { get; }
        public ICommand CloseAccessPopupCommand { get; }

        public RolesViewModel(IRoleApiClient? rolesApi = null)
        {
            _rolesApi = rolesApi ?? new RolesApiClient();

            LoadCommand = new RelayCommand(_ => _ = LoadAsync());
            NewCommand = new RelayCommand(_ => { SelectedRole = null; ClearForm(); });
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(r => _ = DeleteAsync(r as RoleManagementDto));
            ViewAccessCommand = new RelayCommand(r => _ = OpenAccessPopupAsync(r as RoleManagementDto));
            SaveAccessCommand = new RelayCommand(_ => _ = SaveAccessAsync());
            CloseAccessPopupCommand = new RelayCommand(_ => IsAccessPopupOpen = false);
        }

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
                            Id = r.Id,
                            Name = r.Name,
                            Description = r.Description,
                            BadgeColor = r.BadgeColor,
                            BadgeTextColor = r.BadgeTextColor,
                            IsSystem = r.IsSystem,
                            IsActive = r.IsActive
                        });
                });
                SetStatus($"✓ {Roles.Count} role(s) loaded.", true);
                if (SelectedRole == null)
                {
                   // ClearPermissions();
                }
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while loading roles.", false); }
            finally { IsBusy = false; }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FormName))
            { SetStatus("✗ Role name is required.", false); return; }

            IsBusy = true;
            try
            {
                if (_selectedRole == null)
                {
                    await _rolesApi.CreateRoleAsync(new CreateRoleRequest
                    {
                        Name = FormName.Trim(),
                        Description = FormDescription.Trim(),
                        BadgeColor = FormBadgeColor.Trim(),
                        BadgeTextColor = FormBadgeTextColor.Trim()
                    });
                    SetStatus($"✓ Role '{FormName}' created.", true);
                }
                else
                {
                    await _rolesApi.UpdateRoleAsync(_selectedRole.Id, new UpdateRoleRequest
                    {
                        Description = FormDescription.Trim(),
                        BadgeColor = FormBadgeColor.Trim(),
                        BadgeTextColor = FormBadgeTextColor.Trim(),
                        IsActive = FormIsActive
                    });
                    SetStatus($"✓ Role '{_selectedRole.Name}' updated.", true);
                }

                ClearForm();
                await LoadAsync();
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while saving role.", false); }
            finally { IsBusy = false; }
        }

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

        private async Task LoadMenuAccessAsync(int roleId)
        {
            try
            {
                var items = await _rolesApi.GetRoleMenuAccessAsync(roleId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MenuAccessItems.Clear();
                    foreach (var item in items)
                    {
                        MenuAccessItems.Add(item);
                    }
                });
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while loading role menu access.", false); }
        }

        private async Task OpenAccessPopupAsync(RoleManagementDto? role)
        {
            if (role == null) return;

            await LoadMenuAccessAsync(role.Id);
            _accessRoleId = role.Id;
            AccessPopupTitle = $"{role.Name} Access";
            IsAccessPopupOpen = true;
        }

        private async Task SaveAccessAsync()
        {
            if (!_accessRoleId.HasValue) return;

            IsBusy = true;
            try
            {
                var request = new UpdateRoleMenuAccessRequest();
                foreach (var item in MenuAccessItems)
                {
                    request.Items.Add(new RoleMenuAccessUpdateItem
                    {
                        MenuId = item.MenuId,
                        CanView = item.CanView,
                        CanEdit = item.CanEdit,
                        CanModify = item.CanModify,
                        CanDelete = item.CanDelete
                    });
                }

                await _rolesApi.UpdateRoleMenuAccessAsync(_accessRoleId.Value, request);
                SetStatus("✓ Role access updated.", true);
                IsAccessPopupOpen = false;
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while saving role access.", false); }
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
            FormName = r.Name;
            FormDescription = r.Description ?? string.Empty;
            FormBadgeColor = r.BadgeColor;
            FormBadgeTextColor = r.BadgeTextColor;
            FormIsActive = r.IsActive;
        }

        private void ClearForm()
        {
            FormName = string.Empty;
            FormDescription = string.Empty;
            FormBadgeColor = "#22FFFFFF";
            FormBadgeTextColor = "#FFFFFF";
            FormIsActive = true;
            MenuAccessItems.Clear();
            _accessRoleId = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
