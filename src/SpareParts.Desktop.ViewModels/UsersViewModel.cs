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
    // ── ViewModel ─────────────────────────────────────────────────────────────
    public class UsersViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<UserManagementDto> Users { get; } = new();

        // ── Form fields ───────────────────────────────────────────────────────
        private UserManagementDto? _selectedUser;
        public UserManagementDto? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                OnPropertyChanged(nameof(IsEditing));
                if (value != null) PopulateForm(value);
            }
        }

        public bool IsEditing => _selectedUser != null;

        private string _formUsername = string.Empty;
        private string _formFullName = string.Empty;
        private string _formEmail    = string.Empty;
        private string _formPassword = string.Empty;
        private string _formRole     = "Cashier";
        private bool   _formIsActive = true;

        public string FormUsername { get => _formUsername; set { _formUsername = value; OnPropertyChanged(nameof(FormUsername)); } }
        public string FormFullName { get => _formFullName; set { _formFullName = value; OnPropertyChanged(nameof(FormFullName)); } }
        public string FormEmail    { get => _formEmail;    set { _formEmail    = value; OnPropertyChanged(nameof(FormEmail)); } }
        public string FormPassword { get => _formPassword; set { _formPassword = value; OnPropertyChanged(nameof(FormPassword)); } }
        public string FormRole     { get => _formRole;     set { _formRole     = value; OnPropertyChanged(nameof(FormRole)); } }
        public bool   FormIsActive { get => _formIsActive; set { _formIsActive = value; OnPropertyChanged(nameof(FormIsActive)); } }

        public string[] Roles { get; } = { "Admin", "Manager", "Cashier" };

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
        private readonly IUserApiClient _usersApi;

        public ICommand LoadCommand       { get; }
        public ICommand NewCommand        { get; }
        public ICommand SaveCommand       { get; }
        public ICommand DeactivateCommand { get; }

        public UsersViewModel(IUserApiClient? usersApi = null)
        {
            _usersApi = usersApi ?? new UsersApiClient();

            LoadCommand       = new RelayCommand(_ => _ = LoadAsync());
            NewCommand        = new RelayCommand(_ => ClearForm());
            SaveCommand       = new RelayCommand(_ => _ = SaveAsync());
            DeactivateCommand = new RelayCommand(u => _ = DeactivateAsync(u as UserManagementDto));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            IsBusy = true;
            Status = string.Empty;
            try
            {
                var list = await _usersApi.GetUsersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var u in list) Users.Add(UserManagementDto.FromUser(u));
                });
                Status = $"{Users.Count} user(s) loaded.";
            }
            catch (Exception ex) { Status = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Save (Create or Update) ───────────────────────────────────────────
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FormFullName))
            { Status = "Full name is required."; return; }

            IsBusy = true;
            try
            {
                if (_selectedUser == null)
                {
                    if (string.IsNullOrWhiteSpace(FormUsername))
                    { Status = "Username is required."; return; }
                    if (string.IsNullOrWhiteSpace(FormPassword))
                    { Status = "Password is required for new users."; return; }

                    await _usersApi.CreateUserAsync(new CreateUserRequest
                    {
                        Username = FormUsername.Trim(),
                        FullName = FormFullName.Trim(),
                        Email    = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail.Trim(),
                        Password = FormPassword,
                        Role     = FormRole
                    });
                    Status = $"User '{FormUsername}' created.";
                }
                else
                {
                    await _usersApi.UpdateUserAsync(_selectedUser.Id, new UpdateUserRequest
                    {
                        FullName    = FormFullName.Trim(),
                        Email       = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail.Trim(),
                        Role        = FormRole,
                        IsActive    = FormIsActive,
                        NewPassword = string.IsNullOrWhiteSpace(FormPassword) ? null : FormPassword
                    });
                    Status = $"User '{_selectedUser.Username}' updated.";
                }

                ClearForm();
                await LoadAsync();
            }
            catch (Exception ex) { Status = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        // ── Deactivate ────────────────────────────────────────────────────────
        private async Task DeactivateAsync(UserManagementDto? user)
        {
            if (user == null) return;
            IsBusy = true;
            try
            {
                await _usersApi.DeleteUserAsync(user.Id);
                Status = $"User '{user.Username}' deactivated.";
                await LoadAsync();
            }
            catch (Exception ex) { Status = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void PopulateForm(UserManagementDto u)
        {
            FormUsername = u.Username;
            FormFullName = u.FullName;
            FormEmail    = u.Email ?? string.Empty;
            FormPassword = string.Empty; // never pre-fill password
            FormRole     = u.Role;
            FormIsActive = u.IsActive;
        }

        private void ClearForm()
        {
            SelectedUser = null;
            FormUsername = FormFullName = FormEmail = FormPassword = string.Empty;
            FormRole     = "Cashier";
            FormIsActive = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
