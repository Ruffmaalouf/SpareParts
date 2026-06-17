using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ListingBoostViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _durationDays = string.Empty;
        private string _fee = string.Empty;
        private string _paymentReference = string.Empty;

        public ListingBoostViewModel(ICrudApiClient crudApi) : base(crudApi, "Load listing boosts.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            CancelCommand = new RelayCommand(_ => CancelAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string DurationDays
        {
            get => _durationDays;
            set { _durationDays = value; OnPropertyChanged(nameof(DurationDays)); }
        }

        public string Fee
        {
            get => _fee;
            set { _fee = value; OnPropertyChanged(nameof(Fee)); }
        }

        public string PaymentReference
        {
            get => _paymentReference;
            set { _paymentReference = value; OnPropertyChanged(nameof(PaymentReference)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/listing-boosts");
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

        public async Task CreateAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0,
                    durationDays = ParseIntOrNull(DurationDays) ?? 0,
                    fee = ParseDecimalOrNull(Fee),
                    paymentReference = PaymentReference
                };
                await CrudApi.PostAsync("/api/listing-boosts", payload);
                Status = "Listing boost created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                PartId = string.Empty;
                DurationDays = string.Empty;
                Fee = string.Empty;
                PaymentReference = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task CancelAsync()
        {
            var id = GetSelectedId();
            if (id == null)
            {
                Status = "Select a row first.";
                StatusBrush = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            try
            {
                await CrudApi.PutAsync($"/api/listing-boosts/{id}/cancel", new { });
                Status = "Listing boost cancelled.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
        private static decimal? ParseDecimalOrNull(string? value) => decimal.TryParse(value, out var result) ? result : null;
    }
}
