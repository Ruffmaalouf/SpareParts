using Microsoft.Win32;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Growth;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Scanning;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class GrowthLabViewModel : INotifyPropertyChanged
    {
        private readonly IGrowthApiClient _growthApi;
        private readonly IPartsApiClient _partsApi;
        private readonly ICrudApiClient _crudApi;
        private string _status = "Open Money Finder to load today's opportunities.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;
        private DonorCarTreasureDto? _selectedDonorCar;
        private AuctionProfitSimulationDto? _auctionResult;
        private VoiceQuoteDto? _voiceQuote;
        private VisualPartMatchDto? _selectedVisualMatch;
        private string _picturePath = string.Empty;

        public GrowthLabViewModel(
            IGrowthApiClient growthApi,
            IPartsApiClient partsApi,
            ICrudApiClient crudApi)
        {
            _growthApi = growthApi;
            _partsApi = partsApi;
            _crudApi = crudApi;

            RefreshCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            SimulateAuctionCommand = new RelayCommand(_ => SimulateAuctionAsync().SafeFireAndForget(HandleBackgroundException));
            BuildVoiceQuoteCommand = new RelayCommand(_ => BuildVoiceQuoteAsync().SafeFireAndForget(HandleBackgroundException));
            SearchPictureCommand = new RelayCommand(_ => SearchPictureAsync().SafeFireAndForget(HandleBackgroundException));
            CreateReverseRequestCommand = new RelayCommand(_ => CreateReverseRequestAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<MetricTile> MetricTiles { get; } = new();
        public ObservableCollection<MoneyActionDto> MoneyActions { get; } = new();
        public ObservableCollection<DonorCarTreasureDto> DonorCars { get; } = new();
        public ObservableCollection<TeardownQueueItemDto> TeardownQueue { get; } = new();
        public ObservableCollection<DuplicatePartGroupDto> DuplicateGroups { get; } = new();
        public ObservableCollection<BuyingRadarItemDto> BuyingRadar { get; } = new();
        public ObservableCollection<VoiceQuoteMatchDto> QuoteMatches { get; } = new();
        public ObservableCollection<VisualPartMatchDto> VisualMatches { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand SimulateAuctionCommand { get; }
        public ICommand BuildVoiceQuoteCommand { get; }
        public ICommand SearchPictureCommand { get; }
        public ICommand CreateReverseRequestCommand { get; }

        public string Status
        {
            get => _status;
            private set => SetField(ref _status, value);
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set => SetField(ref _statusBrush, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetField(ref _isLoading, value);
        }

        public DonorCarTreasureDto? SelectedDonorCar
        {
            get => _selectedDonorCar;
            set => SetField(ref _selectedDonorCar, value);
        }

        public AuctionProfitSimulationDto? AuctionResult
        {
            get => _auctionResult;
            private set => SetField(ref _auctionResult, value);
        }

        public VoiceQuoteDto? VoiceQuote
        {
            get => _voiceQuote;
            private set
            {
                if (SetField(ref _voiceQuote, value))
                {
                    OnPropertyChanged(nameof(VoiceDraft));
                }
            }
        }

        public string VoiceDraft => VoiceQuote?.DraftMessage ?? "Build a quote to generate the WhatsApp reply.";

        public VisualPartMatchDto? SelectedVisualMatch
        {
            get => _selectedVisualMatch;
            set
            {
                if (!SetField(ref _selectedVisualMatch, value) || value == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(RequestedPartName))
                {
                    RequestedPartName = value.PartName;
                }

                if (string.IsNullOrWhiteSpace(RequestedOemNumber))
                {
                    RequestedOemNumber = value.OemNumber ?? string.Empty;
                }
            }
        }

        public decimal AuctionPrice { get; set; } = 10000m;
        public decimal Transportation { get; set; } = 500m;
        public decimal Customs { get; set; } = 1000m;
        public decimal Shipping { get; set; } = 500m;
        public decimal Repairs { get; set; }
        public decimal PartOutCost { get; set; } = 500m;
        public decimal DesiredProfit { get; set; } = 2000m;
        public int? AuctionCarModelId { get; set; }
        public int? AuctionModelYear { get; set; }

        public string VoiceTranscript { get; set; } = string.Empty;
        public string VoiceCustomerName { get; set; } = string.Empty;
        public string VoiceCustomerPhone { get; set; } = string.Empty;

        public string PictureHint { get; set; } = string.Empty;
        public string PicturePathDisplay => string.IsNullOrWhiteSpace(_picturePath) ? "No picture selected." : _picturePath;
        public string RequestCustomerName { get; set; } = string.Empty;
        public string RequestCustomerPhone { get; set; } = string.Empty;
        public string RequestedPartName { get; set; } = string.Empty;
        public string RequestedOemNumber { get; set; } = string.Empty;
        public string VehicleDetails { get; set; } = string.Empty;
        public string RequestNotes { get; set; } = string.Empty;
        public int RequestQuantity { get; set; } = 1;

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            SetStatus("Loading Money Finder from live stock, requests, and donor cars...", Brushes.LightGoldenrodYellow);

            try
            {
                var briefing = await _growthApi.GetBriefingAsync();
                Replace(MoneyActions, briefing.MoneyActions);
                Replace(DonorCars, briefing.DonorCars);
                Replace(TeardownQueue, briefing.TeardownQueue);
                Replace(DuplicateGroups, briefing.DuplicateGroups);
                Replace(BuyingRadar, briefing.BuyingRadar);
                SelectedDonorCar = DonorCars.FirstOrDefault();
                RebuildMetrics(briefing);
                SetStatus($"Money Finder refreshed at {briefing.GeneratedAt.ToLocalTime():HH:mm}.", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                SetStatus($"Money Finder could not load: {ex.Message}", Brushes.IndianRed);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SimulateAuctionAsync()
        {
            IsLoading = true;
            SetStatus("Simulating donor-car auction profit...", Brushes.LightGoldenrodYellow);

            try
            {
                AuctionResult = await _growthApi.SimulateAuctionAsync(new AuctionProfitSimulationRequest
                {
                    CarModelId = AuctionCarModelId,
                    ModelYear = AuctionModelYear,
                    AuctionPrice = AuctionPrice,
                    Transportation = Transportation,
                    Customs = Customs,
                    Shipping = Shipping,
                    Repairs = Repairs,
                    PartOutCost = PartOutCost,
                    DesiredProfit = DesiredProfit
                });
                SetStatus("Auction simulation ready.", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                SetStatus($"Auction simulation failed: {ex.Message}", Brushes.IndianRed);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BuildVoiceQuoteAsync()
        {
            IsLoading = true;
            SetStatus("Turning the WhatsApp voice-note transcript into a quote...", Brushes.LightGoldenrodYellow);

            try
            {
                VoiceQuote = await _growthApi.BuildVoiceQuoteAsync(new VoiceQuoteRequest
                {
                    Transcript = VoiceTranscript,
                    CustomerName = VoiceCustomerName,
                    CustomerPhone = VoiceCustomerPhone
                });
                Replace(QuoteMatches, VoiceQuote.Matches);
                SetStatus($"Voice-to-quote found {QuoteMatches.Count:N0} catalog match(es).", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                QuoteMatches.Clear();
                SetStatus($"Voice-to-quote failed: {ex.Message}", Brushes.IndianRed);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchPictureAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Reverse storefront picture search",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.webp;*.heic;*.heif)|*.jpg;*.jpeg;*.png;*.webp;*.heic;*.heif|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _picturePath = dialog.FileName;
            OnPropertyChanged(nameof(PicturePathDisplay));
            IsLoading = true;
            SetStatus("Searching the catalog from the counter picture...", Brushes.LightGoldenrodYellow);

            try
            {
                var response = await _partsApi.SearchPartsByImageAsync(_picturePath, PictureHint, 8);
                Replace(VisualMatches, response.Matches);
                SelectedVisualMatch = VisualMatches.FirstOrDefault();
                SetStatus(
                    VisualMatches.Count == 0
                        ? "No catalog matches found. Create an unmatched request for the workshop."
                        : $"Picture search found {VisualMatches.Count:N0} catalog match(es).",
                    VisualMatches.Count == 0 ? Brushes.LightGoldenrodYellow : Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                VisualMatches.Clear();
                SelectedVisualMatch = null;
                SetStatus($"Picture search failed: {ex.Message}", Brushes.IndianRed);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateReverseRequestAsync()
        {
            IsLoading = true;
            SetStatus("Creating the reverse storefront workshop request...", Brushes.LightGoldenrodYellow);

            try
            {
                var notes = string.IsNullOrWhiteSpace(RequestNotes)
                    ? "Reverse storefront request created from WPF."
                    : $"Reverse storefront request created from WPF. {RequestNotes.Trim()}";
                if (!string.IsNullOrWhiteSpace(_picturePath))
                {
                    notes = $"{notes} Picture: {_picturePath}";
                }

                var requestId = await _crudApi.PostAsync<int>("api/partrequests", new CreatePartRequestItemRequest
                {
                    PartId = SelectedVisualMatch?.PartId,
                    CustomerName = RequestCustomerName,
                    CustomerPhone = RequestCustomerPhone,
                    RequestedPartName = RequestedPartName,
                    RequestedOemNumber = RequestedOemNumber,
                    VehicleDetails = VehicleDetails,
                    Quantity = RequestQuantity,
                    Notes = notes
                });

                SetStatus($"Reverse storefront request #{requestId:N0} created for the workshop.", Brushes.LightGreen);
                ClearReverseRequest();
                IsLoading = false;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                SetStatus($"Could not create reverse storefront request: {ex.Message}", Brushes.IndianRed);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearReverseRequest()
        {
            _picturePath = string.Empty;
            PictureHint = string.Empty;
            RequestCustomerName = string.Empty;
            RequestCustomerPhone = string.Empty;
            RequestedPartName = string.Empty;
            RequestedOemNumber = string.Empty;
            VehicleDetails = string.Empty;
            RequestNotes = string.Empty;
            RequestQuantity = 1;
            VisualMatches.Clear();
            SelectedVisualMatch = null;

            OnPropertyChanged(nameof(PicturePathDisplay));
            OnPropertyChanged(nameof(PictureHint));
            OnPropertyChanged(nameof(RequestCustomerName));
            OnPropertyChanged(nameof(RequestCustomerPhone));
            OnPropertyChanged(nameof(RequestedPartName));
            OnPropertyChanged(nameof(RequestedOemNumber));
            OnPropertyChanged(nameof(VehicleDetails));
            OnPropertyChanged(nameof(RequestNotes));
            OnPropertyChanged(nameof(RequestQuantity));
        }

        private void RebuildMetrics(GrowthBriefingDto briefing)
        {
            Replace(MetricTiles, new[]
            {
                new MetricTile("Unlockable", FormatMoney(briefing.UnlockableValue, briefing.Currency), "Tonight's opportunity value", Accent("#FF81C784")),
                new MetricTile("Money actions", briefing.MoneyActions.Count.ToString("N0", CultureInfo.CurrentCulture), "Calls, pricing, and stock moves", Accent("#FFFFB74D")),
                new MetricTile("Donor cars", briefing.DonorCars.Count.ToString("N0", CultureInfo.CurrentCulture), "Treasure maps ready", Accent("#FF64B5F6")),
                new MetricTile("Teardown queue", briefing.TeardownQueue.Count.ToString("N0", CultureInfo.CurrentCulture), "Next workshop jobs", Accent("#FFBA68C8")),
                new MetricTile("Buying signals", briefing.BuyingRadar.Count.ToString("N0", CultureInfo.CurrentCulture), "Parts worth sourcing", Accent("#FFFF7043"))
            });
        }

        private static string FormatMoney(decimal amount, string? currency)
            => $"{(string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant())} {amount:N2}";

        private static SolidColorBrush Accent(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void SetStatus(string message, Brush brush)
        {
            Status = message;
            StatusBrush = brush;
        }

        private void HandleBackgroundException(Exception ex)
        {
            IsLoading = false;
            SetStatus(ex.Message, Brushes.IndianRed);
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
