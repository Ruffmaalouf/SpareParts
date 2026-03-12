using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        // ── Bound properties ──────────────────────────────────────────────────
        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); ClearError(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
                OnPropertyChanged(nameof(HasError));
            }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsIdle)); }
        }
        public bool IsIdle => !_isLoading;

        private string _statusDot   = "●";
        private string _statusText  = "Connecting to API…";
        private string _statusColor = "#FF9E9EA5";

        public string StatusDot   { get => _statusDot;   set { _statusDot   = value; OnPropertyChanged(nameof(StatusDot)); } }
        public string StatusText  { get => _statusText;  set { _statusText  = value; OnPropertyChanged(nameof(StatusText)); } }
        public string StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(nameof(StatusColor)); } }

        // ── Event raised when login succeeds ──────────────────────────────────
        public event Action<LoginResponse>? LoginSucceeded;

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(pwd => ExecuteLoginAsync(pwd as string ?? string.Empty));
            _ = CheckApiAsync();
        }

        // ── Login via API ─────────────────────────────────────────────────────
        private async void ExecuteLoginAsync(string password)
        {
            if (IsLoading) return;
            ClearError();

            if (string.IsNullOrWhiteSpace(Username))  { ErrorMessage = "Please enter your username."; return; }
            if (string.IsNullOrWhiteSpace(password))  { ErrorMessage = "Please enter your password."; return; }

            IsLoading = true;
            try
            {
                var result = await ApiClient.Instance.LoginAsync(Username.Trim(), password);

                // Store token in ApiClient — all future calls use it
                ApiClient.Instance.SetToken(result.Token);

                // Store session info globally
                SessionContext.CurrentUser = new SessionUser
                {
                    UserId   = result.UserId,
                    FullName = result.FullName,
                    Role     = result.Role,
                    Token    = result.Token,
                    ExpiresAt = result.ExpiresAt
                };

                LoginSucceeded?.Invoke(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Cannot reach API: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Ping API health indicator ─────────────────────────────────────────
        private async Task CheckApiAsync()
        {
            StatusText  = "Connecting to API…";
            StatusColor = "#FF9E9EA5";
            StatusDot   = "●";

            bool alive = await ApiClient.Instance.PingAsync();

            if (alive)
            {
                StatusText  = "API online";
                StatusColor = "#FF4CAF50";
            }
            else
            {
                StatusText  = "API offline — start SpareParts.Api";
                StatusColor = "#FFE53935";
            }
        }

        private void ClearError() => ErrorMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Session holder ────────────────────────────────────────────────────────
    public class SessionUser
    {
        public int      UserId    { get; set; }
        public string   FullName  { get; set; } = string.Empty;
        public string   Role      { get; set; } = string.Empty;
        public string   Token     { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public static class SessionContext
    {
        public static SessionUser? CurrentUser { get; set; }
    }
}
