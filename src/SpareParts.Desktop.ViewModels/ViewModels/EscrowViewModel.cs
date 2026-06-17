using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class EscrowViewModel : JsonListViewModelBase
    {
        private string _buyerName = string.Empty;
        private string _buyerPhone = string.Empty;
        private string _sellerName = string.Empty;
        private string _sellerPhone = string.Empty;
        private string _amount = string.Empty;
        private string _currency = string.Empty;
        private string _notes = string.Empty;
        private string _partId = string.Empty;
        private string _newStatus = string.Empty;

        public EscrowViewModel(ICrudApiClient crudApi) : base(crudApi, "Load escrow transactions.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            UpdateStatusCommand = new RelayCommand(_ => UpdateStatusAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand UpdateStatusCommand { get; }

        public string BuyerName
        {
            get => _buyerName;
            set { _buyerName = value; OnPropertyChanged(nameof(BuyerName)); }
        }

        public string BuyerPhone
        {
            get => _buyerPhone;
            set { _buyerPhone = value; OnPropertyChanged(nameof(BuyerPhone)); }
        }

        public string SellerName
        {
            get => _sellerName;
            set { _sellerName = value; OnPropertyChanged(nameof(SellerName)); }
        }

        public string SellerPhone
        {
            get => _sellerPhone;
            set { _sellerPhone = value; OnPropertyChanged(nameof(SellerPhone)); }
        }

        public string Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(nameof(Amount)); }
        }

        public string Currency
        {
            get => _currency;
            set { _currency = value; OnPropertyChanged(nameof(Currency)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string NewStatus
        {
            get => _newStatus;
            set { _newStatus = value; OnPropertyChanged(nameof(NewStatus)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/escrow");
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
                    buyerName = BuyerName,
                    buyerPhone = BuyerPhone,
                    sellerName = SellerName,
                    sellerPhone = SellerPhone,
                    amount = ParseDecimalOrNull(Amount) ?? 0,
                    currency = string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency,
                    notes = Notes,
                    partId = ParseIntOrNull(PartId)
                };
                await CrudApi.PostAsync("/api/escrow", payload);
                Status = "Escrow transaction created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                BuyerName = string.Empty;
                BuyerPhone = string.Empty;
                SellerName = string.Empty;
                SellerPhone = string.Empty;
                Amount = string.Empty;
                Currency = string.Empty;
                Notes = string.Empty;
                PartId = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task UpdateStatusAsync()
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
                var payload = new { status = NewStatus };
                await CrudApi.PutAsync($"/api/escrow/{id}/status", payload);
                Status = "Escrow status updated.";
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
