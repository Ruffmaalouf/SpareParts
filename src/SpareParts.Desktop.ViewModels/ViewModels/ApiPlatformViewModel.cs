using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ApiPlatformViewModel : JsonListViewModelBase
    {
        private string _name = string.Empty;
        private string _scopes = string.Empty;
        private string _plainKey = string.Empty;

        public ApiPlatformViewModel(ICrudApiClient crudApi) : base(crudApi, "Manage API keys for third-party integrations.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            RevokeCommand = new RelayCommand(_ => RevokeAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand RevokeCommand { get; }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Scopes
        {
            get => _scopes;
            set { _scopes = value; OnPropertyChanged(nameof(Scopes)); }
        }

        public string PlainKey
        {
            get => _plainKey;
            private set { _plainKey = value; OnPropertyChanged(nameof(PlainKey)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/api-platform/keys");
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"Loaded {items.Count} API keys.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CreateAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Status = "Name is required.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsLoading = true;
            try
            {
                var payload = new
                {
                    name = Name.Trim(),
                    scopes = ParseScopes()
                };
                var created = await CrudApi.PostAsync<JsonElement>("/api/api-platform/keys", payload);
                PlainKey = created.TryGetProperty("plainKey", out var plainKey) ? plainKey.GetString() ?? string.Empty : string.Empty;
                Status = string.IsNullOrWhiteSpace(PlainKey)
                    ? "API key created."
                    : "API key created. Copy the plain key now.";
                StatusBrush = Brushes.LightGreen;
                Name = string.Empty;
                Scopes = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RevokeAsync()
        {
            var keyId = GetSelectedId();
            if (!keyId.HasValue)
            {
                Status = "Select an API key row first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsLoading = true;
            try
            {
                await CrudApi.DeleteAsync($"/api/api-platform/keys/{keyId.Value}");
                PlainKey = string.Empty;
                Status = "API key revoked.";
                StatusBrush = Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<string> ParseScopes() => Scopes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .ToList();
    }
}
