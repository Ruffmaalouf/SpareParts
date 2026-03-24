using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        public string FormName           { get => _formName;           set { _formName           = value; OnPropertyChanged(nameof(FormName)); } }
        public string FormDescription    { get => _formDescription;    set { _formDescription    = value; OnPropertyChanged(nameof(FormDescription)); } }
        public string FormBadgeColor     { get => _formBadgeColor;     set { _formBadgeColor     = value; OnPropertyChanged(nameof(FormBadgeColor)); } }
        public string FormBadgeTextColor { get => _formBadgeTextColor; set { _formBadgeTextColor = value; OnPropertyChanged(nameof(FormBadgeTextColor)); } }
        public bool   FormIsActive       { get => _formIsActive;       set { _formIsActive       = value; OnPropertyChanged(nameof(FormIsActive)); } }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
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

        public RolesViewModel(IRoleApiClient? rolesApi = null)
        {
            _rolesApi = rolesApi ?? new RolesApiClient();

            LoadCommand   = new RelayCommand(_ => _ = LoadAsync());
            NewCommand    = new RelayCommand(_ => { SelectedRole = null; ClearForm(); });
            SaveCommand   = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(r => _ = DeleteAsync(r as RoleManagementDto));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            IsBusy = true;
            Status = string.Empty;
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
                Status = $"✓ {Roles.Count} role(s) loaded.";
            }
            catch (Exception ex) { Status = $"✗ {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Save (create or update) ───────────────────────────────────────────
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FormName))
            { Status = "✗ Role name is required."; return; }

            IsBusy = true;
            try
            {
                if (_selectedRole == null)
                {
                    // Create
                    await _rolesApi.CreateRoleAsync(new CreateRoleRequest
                    {
                        Name           = FormName.Trim(),
                        Description    = FormDescription.Trim(),
                        BadgeColor     = FormBadgeColor.Trim(),
                        BadgeTextColor = FormBadgeTextColor.Trim()
                    });
                    Status = $"✓ Role '{FormName}' created.";
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
                    Status = $"✓ Role '{_selectedRole.Name}' updated.";
                }

                ClearForm();
                await LoadAsync();
            }
            catch (Exception ex) { Status = $"✗ {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private async Task DeleteAsync(RoleManagementDto? role)
        {
            if (role == null) return;
            if (role.IsSystem)
            { Status = "✗ Built-in system roles cannot be deleted."; return; }

            IsBusy = true;
            try
            {
                await _rolesApi.DeleteRoleAsync(role.Id);
                Status = $"✓ Role '{role.Name}' deactivated.";
                await LoadAsync();
            }
            catch (Exception ex) { Status = $"✗ {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void PopulateForm(RoleManagementDto r)
        {
            FormName           = r.Name;
            FormDescription    = r.Description ?? string.Empty;
            FormBadgeColor     = r.BadgeColor;
            FormBadgeTextColor = r.BadgeTextColor;
            FormIsActive       = r.IsActive;
        }

        private void ClearForm()
        {
            FormName           = string.Empty;
            FormDescription    = string.Empty;
            FormBadgeColor     = "#22FFFFFF";
            FormBadgeTextColor = "#FFFFFF";
            FormIsActive       = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
