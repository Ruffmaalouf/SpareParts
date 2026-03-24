using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

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

        private string _statusDot  = "●";
        private string _statusText = "Connecting to API…";
        private Brush  _statusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x9E, 0x9E, 0xA5));

        public string StatusDot
        {
            get => _statusDot;
            set { _statusDot = value; OnPropertyChanged(nameof(StatusDot)); }
        }
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }
        /// <summary>Foreground brush for the status dot — bind directly to Foreground in XAML.</summary>
        public Brush StatusBrush
        {
            get => _statusBrush;
            set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        // ── Event raised when login succeeds ──────────────────────────────────
        public event Action<LoginResponse>? LoginSucceeded;

        // ── Dependencies / Commands ───────────────────────────────────────────
        private readonly IAuthApiClient _authApi;
        private readonly IApiSessionClient _sessionApi;

        public ICommand LoginCommand { get; }

        public LoginViewModel(IAuthApiClient? authApi = null, IApiSessionClient? sessionApi = null)
        {
            _authApi = authApi ?? new AuthApiClient();
            _sessionApi = sessionApi ?? new ApiSessionClient();

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
                var result = await _authApi.LoginAsync(Username.Trim(), password);

                // Store token in ApiClient — all future calls use it
                _sessionApi.SetToken(result.Token);

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
            StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x9E, 0x9E, 0xA5));

            bool alive = await _authApi.PingAsync();

            if (alive)
            {
                StatusText  = "API online";
                StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50)); // green
            }
            else
            {
                StatusText  = "API offline — start SpareParts.Api";
                StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE5, 0x39, 0x35)); // red
            }
        }

        private void ClearError() => ErrorMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
