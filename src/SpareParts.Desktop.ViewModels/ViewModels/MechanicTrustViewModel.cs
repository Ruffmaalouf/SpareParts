using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class MechanicTrustViewModel : JsonListViewModelBase
    {
        private string _userId = string.Empty;
        private string? _specialtyBrands;
        private string? _specialtyTypes;
        private string _verifyUserId = string.Empty;

        public MechanicTrustViewModel(ICrudApiClient crudApi) : base(crudApi, "Load mechanic trust profiles.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            SaveCommand = new RelayCommand(_ => SaveAsync().SafeFireAndForget(HandleException));
            VerifyCommand = new RelayCommand(_ => VerifyAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand VerifyCommand { get; }

        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(nameof(UserId)); }
        }

        public string? SpecialtyBrands
        {
            get => _specialtyBrands;
            set { _specialtyBrands = value; OnPropertyChanged(nameof(SpecialtyBrands)); }
        }

        public string? SpecialtyTypes
        {
            get => _specialtyTypes;
            set { _specialtyTypes = value; OnPropertyChanged(nameof(SpecialtyTypes)); }
        }

        public string VerifyUserId
        {
            get => _verifyUserId;
            set { _verifyUserId = value; OnPropertyChanged(nameof(VerifyUserId)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/mechanic-trust");
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} item(s) loaded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var payload = new
                {
                    userId = ParseIntOrNull(UserId) ?? 0,
                    specialtyBrands = SpecialtyBrands,
                    specialtyTypes = SpecialtyTypes
                };
                await CrudApi.PostAsync("/api/mechanic-trust", payload);
                Status = "Mechanic trust profile saved.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                UserId = string.Empty;
                SpecialtyBrands = string.Empty;
                SpecialtyTypes = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task VerifyAsync()
        {
            try
            {
                await CrudApi.PostAsync($"/api/mechanic-trust/user/{VerifyUserId}/verify", new { });
                Status = "Mechanic verified.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                VerifyUserId = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
