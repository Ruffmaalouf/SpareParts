using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartInsuranceViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _salesInvoiceId = string.Empty;
        private string _insuranceOptionId = string.Empty;
        private string _resultText = string.Empty;

        public PartInsuranceViewModel(ICrudApiClient crudApi) : base(crudApi, "Load insurance options.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            ApplyAddonCommand = new RelayCommand(_ => ApplyAddonAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand ApplyAddonCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string SalesInvoiceId
        {
            get => _salesInvoiceId;
            set { _salesInvoiceId = value; OnPropertyChanged(nameof(SalesInvoiceId)); }
        }

        public string InsuranceOptionId
        {
            get => _insuranceOptionId;
            set { _insuranceOptionId = value; OnPropertyChanged(nameof(InsuranceOptionId)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/part-insurance/options");
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

        public async Task ApplyAddonAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0,
                    salesInvoiceId = ParseIntOrNull(SalesInvoiceId) ?? 0,
                    insuranceOptionId = ParseIntOrNull(InsuranceOptionId) ?? 0
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/part-insurance/addon", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Insurance add-on applied.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
