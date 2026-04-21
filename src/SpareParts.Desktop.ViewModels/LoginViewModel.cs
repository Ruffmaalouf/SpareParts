using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
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
        private string _serviceStatusText = string.Empty;
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

        public string ServiceStatusText
        {
            get => _serviceStatusText;
            set { _serviceStatusText = value; OnPropertyChanged(nameof(ServiceStatusText)); }
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

        public LoginViewModel(
            IAuthApiClient authApi,
            IApiSessionClient sessionApi)
        {
            _authApi = authApi;
            _sessionApi = sessionApi;

            LoginCommand = new RelayCommand(pwd => ExecuteLoginAsync(pwd as string ?? string.Empty));
            CheckApiAsync().SafeFireAndForget(ex =>
            {
                StatusText = "Unable to check service endpoints.";
                StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE5, 0x39, 0x35));
                ServiceStatusText = $"Health-check failed: {ex.Message}";
            });
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

                var sessionUser = new SessionUser
                {
                    UserId = result.UserId,
                    FullName = result.FullName,
                    Role = result.Role,
                    Token = result.Token,
                    ExpiresAt = result.ExpiresAt
                };

                // Store token in the shared API token store for future API calls
                _sessionApi.SetToken(result.Token);

                // Store session info globally
                SessionContext.CurrentUser = sessionUser;

                try
                {
                    LoginSucceeded?.Invoke(result);
                }
                catch (Exception ex)
                {
                    _sessionApi.ClearToken();
                    SessionContext.CurrentUser = null;
                    ErrorMessage = $"Login succeeded, but the main window could not open: {ex.Message}";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (ApiClientException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Cannot reach API: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                ErrorMessage = "Cannot reach API: the request timed out.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Ping API health indicator ─────────────────────────────────────────
        private async Task CheckApiAsync()
        {
            StatusText  = "Checking service endpoints…";
            StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x9E, 0x9E, 0xA5));
            ServiceStatusText = string.Empty;

            var statuses = await PingAllEndpointsAsync();
            var onlineCount = statuses.Count(x => x.IsOnline);

            ServiceStatusText = string.Join(Environment.NewLine, statuses.Select(status =>
                $"{status.Label}: {(status.IsOnline ? "Online" : "Offline")} ({status.Url})"));

            if (onlineCount == statuses.Count)
            {
                StatusText  = "All API endpoints are online";
                StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50)); // green
            }
            else
            {
                StatusText  = $"{onlineCount}/{statuses.Count} API endpoints online";
                StatusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE5, 0x39, 0x35)); // red
            }
        }

        private async Task<List<EndpointStatus>> PingAllEndpointsAsync()
        {
            var endpoints = new List<EndpointStatus>
            {
                new("Identity", AppSettings.IdentityApiBaseUrl),
                new("Sales", AppSettings.SalesApiBaseUrl),
                new("Purchases", AppSettings.PurchasesApiBaseUrl),
                new("Inventory", AppSettings.InventoryApiBaseUrl),
                new("Catalog", AppSettings.CatalogApiBaseUrl)
            };

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };

            for (var index = 0; index < endpoints.Count; index++)
            {
                var endpoint = endpoints[index];
                endpoint.IsOnline = index == 0
                    ? await _authApi.PingAsync()
                    : await PingHealthEndpointAsync(httpClient, endpoint.Url);
            }

            return endpoints;
        }

        private static async Task<bool> PingHealthEndpointAsync(HttpClient httpClient, string baseUrl)
        {
            try
            {
                var healthUrl = new Uri(new Uri(baseUrl), "api/health");
                using var response = await httpClient.GetAsync(healthUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private sealed class EndpointStatus(string label, string url)
        {
            public string Label { get; } = label;
            public string Url { get; } = url;
            public bool IsOnline { get; set; }
        }

        private void ClearError() => ErrorMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
