using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class NegotiationViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _buyerName = string.Empty;
        private string? _buyerPhone;
        private string _initialOffer = string.Empty;
        private string _offerAmount = string.Empty;

        public NegotiationViewModel(ICrudApiClient crudApi) : base(crudApi, "Load negotiations.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            StartCommand = new RelayCommand(_ => StartAsync().SafeFireAndForget(HandleException));
            MakeOfferCommand = new RelayCommand(_ => MakeOfferAsync().SafeFireAndForget(HandleException));
            AcceptCommand = new RelayCommand(_ => AcceptAsync().SafeFireAndForget(HandleException));
            RejectCommand = new RelayCommand(_ => RejectAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand MakeOfferCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand RejectCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string BuyerName
        {
            get => _buyerName;
            set { _buyerName = value; OnPropertyChanged(nameof(BuyerName)); }
        }

        public string? BuyerPhone
        {
            get => _buyerPhone;
            set { _buyerPhone = value; OnPropertyChanged(nameof(BuyerPhone)); }
        }

        public string InitialOffer
        {
            get => _initialOffer;
            set { _initialOffer = value; OnPropertyChanged(nameof(InitialOffer)); }
        }

        public string OfferAmount
        {
            get => _offerAmount;
            set { _offerAmount = value; OnPropertyChanged(nameof(OfferAmount)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/negotiations");
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

        public async Task StartAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0,
                    buyerName = BuyerName,
                    buyerPhone = BuyerPhone,
                    initialOffer = ParseDecimalOrNull(InitialOffer) ?? 0
                };
                await CrudApi.PostAsync("/api/negotiations", payload);
                Status = "Negotiation started.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                PartId = string.Empty;
                BuyerName = string.Empty;
                BuyerPhone = string.Empty;
                InitialOffer = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task MakeOfferAsync()
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
                var payload = new { amount = ParseDecimalOrNull(OfferAmount) ?? 0, offeredByRole = "Buyer" };
                await CrudApi.PostAsync($"/api/negotiations/{id}/offer", payload);
                Status = "Offer submitted.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task AcceptAsync()
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
                await CrudApi.PostAsync($"/api/negotiations/{id}/accept", new { });
                Status = "Negotiation accepted.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task RejectAsync()
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
                await CrudApi.PostAsync($"/api/negotiations/{id}/reject", new { });
                Status = "Negotiation rejected.";
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
