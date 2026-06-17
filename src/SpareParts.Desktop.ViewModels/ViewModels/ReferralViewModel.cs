using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReferralViewModel : JsonListViewModelBase
    {
        private string _myCode = string.Empty;

        public ReferralViewModel(ICrudApiClient crudApi) : base(crudApi, "Load referrals.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            GenerateCommand = new RelayCommand(_ => GenerateAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand GenerateCommand { get; }

        public string MyCode
        {
            get => _myCode;
            private set { _myCode = value; OnPropertyChanged(nameof(MyCode)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var data = await CrudApi.GetAsync<JsonElement>("/api/referral/my-code");
                MyCode = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("code", out var codeProp)
                    ? codeProp.GetString() ?? string.Empty
                    : data.GetRawText();

                var items = await CrudApi.GetAllAsync<JsonElement>("/api/referral/my-referrals");
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

        public async Task GenerateAsync()
        {
            try
            {
                await CrudApi.PostAsync("/api/referral/generate", new { });
                Status = "Referral code generated.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}
