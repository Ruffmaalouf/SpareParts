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
        private readonly IUserApiClient _usersApi;

        public ICommand LoadCommand       { get; }
        public ICommand NewCommand        { get; }
        public ICommand SaveCommand       { get; }
        public ICommand DeactivateCommand { get; }

        public UsersViewModel(IUserApiClient usersApi)
        {
            _usersApi = usersApi;

            LoadCommand       = new RelayCommand(_ => _ = LoadAsync());
            NewCommand        = new RelayCommand(_ => ClearForm());
            SaveCommand       = new RelayCommand(_ => _ = SaveAsync());
            DeactivateCommand = new RelayCommand(u => _ = DeactivateAsync(u as UserManagementDto));
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            IsBusy = true;
            SetStatus(string.Empty, true);
            try
            {
                var list = await _usersApi.GetUsersAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var u in list) Users.Add(UserManagementDto.FromUser(u));
                });
                SetStatus($"✓ {Users.Count} user(s) loaded.", true);
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while loading users.", false); }
            finally { IsBusy = false; }
        }

        // ── Save (Create or Update) ───────────────────────────────────────────
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FormFullName))
            { SetStatus("✗ Full name is required.", false); return; }

            IsBusy = true;
            try
            {
                if (_selectedUser == null)
                {
                    if (string.IsNullOrWhiteSpace(FormUsername))
                    { SetStatus("✗ Username is required.", false); return; }
                    if (string.IsNullOrWhiteSpace(FormPassword))
                    { SetStatus("✗ Password is required for new users.", false); return; }

                    await _usersApi.CreateUserAsync(new CreateUserRequest
                    {
                        Username = FormUsername.Trim(),
                        FullName = FormFullName.Trim(),
                        Email    = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail.Trim(),
                        Password = FormPassword,
                        Role     = FormRole
                    });
                    SetStatus($"✓ User '{FormUsername}' created.", true);
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
                    SetStatus($"✓ User '{_selectedUser.Username}' updated.", true);
                }

                ClearForm();
                await LoadAsync();
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while saving user.", false); }
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
                SetStatus($"✓ User '{user.Username}' deactivated.", true);
                await LoadAsync();
            }
            catch (ApiClientException ex) { SetStatus($"✗ API error ({ex.Code}): {ex.Message}", false); }
            catch (Exception) { SetStatus("✗ Unexpected error while deactivating user.", false); }
            finally { IsBusy = false; }
        }

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.MediumSeaGreen : Brushes.IndianRed;
            AppNotificationCenter.Instance.Publish(message, isSuccess);
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
