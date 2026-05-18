using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Communications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class WhatsAppInboxViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private string _searchText = string.Empty;
        private string _manualName = string.Empty;
        private string _manualPhone = string.Empty;
        private string _composeText = string.Empty;
        private string _status = "Ready.";
        private Brush _statusBrush = Brushes.LightGreen;
        private bool _isLoading;
        private WhatsAppConversationItemViewModel? _selectedConversation;
        private string _campaignSegment = WhatsAppCampaignSegments.AllCustomers;
        private string _campaignLanguage = "ArabicEnglish";
        private bool _campaignIncludeImages = true;
        private string _campaignName = string.Empty;
        private string _campaignNote = string.Empty;
        private string _campaignMessage = string.Empty;
        private int _campaignRecipientCount;
        private int _campaignAttachmentCount;
        private int _campaignSentCount;
        private int _campaignFailedCount;

        public WhatsAppInboxViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadConversationsCommand = new RelayCommand(_ => LoadConversationsAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshMessagesCommand = new RelayCommand(_ => RefreshSelectedThreadAsync().SafeFireAndForget(HandleBackgroundException));
            SendMessageCommand = new RelayCommand(_ => SendMessageAsync().SafeFireAndForget(HandleBackgroundException));
            StartManualConversationCommand = new RelayCommand(_ => StartManualConversation());
            LoadCampaignBuilderCommand = new RelayCommand(_ => LoadCampaignBuilderAsync().SafeFireAndForget(HandleBackgroundException));
            PreviewCampaignCommand = new RelayCommand(_ => PreviewCampaignAsync().SafeFireAndForget(HandleBackgroundException));
            SendCampaignCommand = new RelayCommand(_ => SendCampaignAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public ObservableCollection<WhatsAppConversationItemViewModel> Conversations { get; } = new();
        public ObservableCollection<WhatsAppMessageItemViewModel> Messages { get; } = new();
        public ObservableCollection<CampaignOptionViewModel> CampaignSegments { get; } = new(new[]
        {
            new CampaignOptionViewModel("All customers", WhatsAppCampaignSegments.AllCustomers),
            new CampaignOptionViewModel("Active customers", WhatsAppCampaignSegments.ActiveCustomers),
            new CampaignOptionViewModel("Unpaid customers", WhatsAppCampaignSegments.UnpaidCustomers),
            new CampaignOptionViewModel("Waiting for parts", WhatsAppCampaignSegments.WaitingForParts),
            new CampaignOptionViewModel("Recent buyers", WhatsAppCampaignSegments.RecentBuyers),
            new CampaignOptionViewModel("Inactive customers", WhatsAppCampaignSegments.InactiveCustomers)
        });
        public ObservableCollection<CampaignOptionViewModel> CampaignLanguages { get; } = new(new[]
        {
            new CampaignOptionViewModel("Arabic + English", "ArabicEnglish"),
            new CampaignOptionViewModel("Arabic", "Arabic"),
            new CampaignOptionViewModel("English", "English")
        });
        public ObservableCollection<WhatsAppCampaignAssetItemViewModel> CampaignAssets { get; } = new();
        public ObservableCollection<WhatsAppCampaignRecipientDto> CampaignRecipients { get; } = new();
        public ObservableCollection<WhatsAppCampaignSummaryDto> RecentCampaigns { get; } = new();

        public ICommand LoadConversationsCommand { get; }
        public ICommand RefreshMessagesCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand StartManualConversationCommand { get; }
        public ICommand LoadCampaignBuilderCommand { get; }
        public ICommand PreviewCampaignCommand { get; }
        public ICommand SendCampaignCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public string ManualName
        {
            get => _manualName;
            set
            {
                if (_manualName == value)
                {
                    return;
                }

                _manualName = value;
                OnPropertyChanged(nameof(ManualName));
                OnPropertyChanged(nameof(ActiveRecipientTitle));
                OnPropertyChanged(nameof(ActiveRecipientSubtitle));
            }
        }

        public string ManualPhone
        {
            get => _manualPhone;
            set
            {
                if (_manualPhone == value)
                {
                    return;
                }

                _manualPhone = value;
                OnPropertyChanged(nameof(ManualPhone));
                OnPropertyChanged(nameof(ActiveRecipientSubtitle));
            }
        }

        public string ComposeText
        {
            get => _composeText;
            set
            {
                if (_composeText == value)
                {
                    return;
                }

                _composeText = value;
                OnPropertyChanged(nameof(ComposeText));
            }
        }

        public string CampaignSegment
        {
            get => _campaignSegment;
            set
            {
                if (_campaignSegment == value)
                {
                    return;
                }

                _campaignSegment = value;
                OnPropertyChanged(nameof(CampaignSegment));
            }
        }

        public string CampaignLanguage
        {
            get => _campaignLanguage;
            set
            {
                if (_campaignLanguage == value)
                {
                    return;
                }

                _campaignLanguage = value;
                OnPropertyChanged(nameof(CampaignLanguage));
            }
        }

        public bool CampaignIncludeImages
        {
            get => _campaignIncludeImages;
            set
            {
                if (_campaignIncludeImages == value)
                {
                    return;
                }

                _campaignIncludeImages = value;
                OnPropertyChanged(nameof(CampaignIncludeImages));
            }
        }

        public string CampaignName
        {
            get => _campaignName;
            set
            {
                if (_campaignName == value)
                {
                    return;
                }

                _campaignName = value;
                OnPropertyChanged(nameof(CampaignName));
            }
        }

        public string CampaignNote
        {
            get => _campaignNote;
            set
            {
                if (_campaignNote == value)
                {
                    return;
                }

                _campaignNote = value;
                OnPropertyChanged(nameof(CampaignNote));
            }
        }

        public string CampaignMessage
        {
            get => _campaignMessage;
            set
            {
                if (_campaignMessage == value)
                {
                    return;
                }

                _campaignMessage = value;
                OnPropertyChanged(nameof(CampaignMessage));
            }
        }

        public int CampaignRecipientCount
        {
            get => _campaignRecipientCount;
            private set
            {
                if (_campaignRecipientCount == value)
                {
                    return;
                }

                _campaignRecipientCount = value;
                OnPropertyChanged(nameof(CampaignRecipientCount));
            }
        }

        public int CampaignAttachmentCount
        {
            get => _campaignAttachmentCount;
            private set
            {
                if (_campaignAttachmentCount == value)
                {
                    return;
                }

                _campaignAttachmentCount = value;
                OnPropertyChanged(nameof(CampaignAttachmentCount));
            }
        }

        public int CampaignSentCount
        {
            get => _campaignSentCount;
            private set
            {
                if (_campaignSentCount == value)
                {
                    return;
                }

                _campaignSentCount = value;
                OnPropertyChanged(nameof(CampaignSentCount));
            }
        }

        public int CampaignFailedCount
        {
            get => _campaignFailedCount;
            private set
            {
                if (_campaignFailedCount == value)
                {
                    return;
                }

                _campaignFailedCount = value;
                OnPropertyChanged(nameof(CampaignFailedCount));
            }
        }

        public WhatsAppConversationItemViewModel? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                if (_selectedConversation == value)
                {
                    return;
                }

                _selectedConversation = value;
                if (value != null)
                {
                    ManualName = value.RecipientName;
                    ManualPhone = value.RecipientPhone;
                    LoadMessagesAsync(value).SafeFireAndForget(HandleBackgroundException);
                }

                OnPropertyChanged(nameof(SelectedConversation));
                OnPropertyChanged(nameof(HasSelectedConversation));
                OnPropertyChanged(nameof(ActiveRecipientTitle));
                OnPropertyChanged(nameof(ActiveRecipientSubtitle));
            }
        }

        public bool HasSelectedConversation => SelectedConversation != null;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value)
                {
                    return;
                }

                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string ActiveRecipientTitle
        {
            get
            {
                if (SelectedConversation != null)
                {
                    return SelectedConversation.RecipientName;
                }

                return string.IsNullOrWhiteSpace(ManualName) ? "New WhatsApp conversation" : ManualName.Trim();
            }
        }

        public string ActiveRecipientSubtitle
        {
            get
            {
                if (SelectedConversation != null)
                {
                    return $"{SelectedConversation.RecipientPhone} · {SelectedConversation.MessageCount:N0} message(s)";
                }

                return string.IsNullOrWhiteSpace(ManualPhone) ? "Enter a phone number to start" : ManualPhone.Trim();
            }
        }

        public async Task LoadConversationsAsync(string? selectPhone = null)
        {
            IsLoading = true;
            Status = "Loading WhatsApp conversations...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var route = "api/communications/conversations?take=100";
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    route += $"&search={Uri.EscapeDataString(SearchText.Trim())}";
                }

                var rows = await _crudApi.GetAllAsync<CommunicationConversationDto>(route);
                var selectedPhone = selectPhone ?? SelectedConversation?.RecipientPhone;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    foreach (var item in rows.Select(dto => new WhatsAppConversationItemViewModel(dto)))
                    {
                        Conversations.Add(item);
                    }

                    var nextSelection = !string.IsNullOrWhiteSpace(selectedPhone)
                        ? Conversations.FirstOrDefault(item => string.Equals(item.RecipientPhone, selectedPhone, StringComparison.Ordinal))
                        : Conversations.FirstOrDefault();

                    SelectedConversation = nextSelection;
                    if (nextSelection == null)
                    {
                        Messages.Clear();
                    }
                });

                Status = Conversations.Count == 0 ? "No WhatsApp conversations yet." : "Conversations loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            catch (Exception ex)
            {
                Status = $"Could not load conversations: {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadCampaignBuilderAsync()
        {
            IsLoading = true;
            Status = "Loading campaign builder...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var assetsTask = _crudApi.GetAllAsync<WhatsAppCampaignAssetDto>("api/communications/campaign-assets");
                var recentTask = _crudApi.GetAllAsync<WhatsAppCampaignSummaryDto>("api/communications/campaigns/recent?take=30");
                await Task.WhenAll(assetsTask, recentTask);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var selectedKeys = CampaignAssets
                        .Where(item => item.IsSelected)
                        .Select(item => item.AssetKey)
                        .ToHashSet(StringComparer.Ordinal);

                    CampaignAssets.Clear();
                    foreach (var asset in assetsTask.Result.Select(dto => new WhatsAppCampaignAssetItemViewModel(dto)))
                    {
                        asset.IsSelected = selectedKeys.Contains(asset.AssetKey);
                        CampaignAssets.Add(asset);
                    }

                    RecentCampaigns.Clear();
                    foreach (var campaign in recentTask.Result)
                    {
                        RecentCampaigns.Add(campaign);
                    }
                });

                Status = "Campaign builder loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            catch (Exception ex)
            {
                Status = $"Could not load campaigns: {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PreviewCampaignAsync()
        {
            IsLoading = true;
            Status = "Generating campaign message...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var response = await _crudApi.PostAsync<WhatsAppCampaignPreviewResponse>(
                    "api/communications/campaign-preview",
                    BuildCampaignRequest());

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CampaignMessage = response.MessageBody;
                    CampaignRecipientCount = response.RecipientCount;
                    CampaignAttachmentCount = response.AttachmentCount;
                    CampaignRecipients.Clear();
                    foreach (var recipient in response.Recipients.Take(24))
                    {
                        CampaignRecipients.Add(recipient);
                    }
                });

                Status = $"Preview ready for {response.RecipientCount:N0} customer(s).";
                StatusBrush = Brushes.LightGreen;
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            catch (Exception ex)
            {
                Status = $"Could not preview campaign: {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SendCampaignAsync()
        {
            IsLoading = true;
            Status = "Sending WhatsApp campaign...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var request = BuildSendCampaignRequest();
                var response = await _crudApi.PostAsync<WhatsAppCampaignSendResponse>(
                    "api/communications/campaigns/send",
                    request);

                CampaignSentCount = response.SentCount + response.PreparedCount;
                CampaignFailedCount = response.FailedCount;
                CampaignRecipientCount = response.RecipientCount;
                CampaignAttachmentCount = response.AttachmentCount;
                CampaignMessage = response.MessageBody;

                await LoadCampaignBuilderAsync();
                await LoadConversationsAsync();

                Status = $"Campaign {response.Status.ToLowerInvariant()}: {CampaignSentCount:N0} prepared/sent, {response.FailedCount:N0} failed.";
                StatusBrush = response.FailedCount > 0 ? Brushes.LightGoldenrodYellow : Brushes.LightGreen;
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            catch (Exception ex)
            {
                Status = $"Could not send campaign: {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private SendWhatsAppCampaignRequest BuildSendCampaignRequest()
        {
            var request = BuildCampaignRequest();
            return new SendWhatsAppCampaignRequest
            {
                Segment = request.Segment,
                CustomerIds = request.CustomerIds,
                PartIds = request.PartIds,
                UsedCarIds = request.UsedCarIds,
                Language = request.Language,
                IncludeImages = request.IncludeImages,
                Note = request.Note,
                MaxRecipients = request.MaxRecipients,
                Name = CampaignName,
                MessageBodyOverride = CampaignMessage
            };
        }

        private WhatsAppCampaignPreviewRequest BuildCampaignRequest()
        {
            var selectedParts = CampaignAssets
                .Where(asset => asset.IsSelected && string.Equals(asset.AssetType, "Part", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.Id)
                .ToList();
            var selectedCars = CampaignAssets
                .Where(asset => asset.IsSelected && string.Equals(asset.AssetType, "Car", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.Id)
                .ToList();

            return new WhatsAppCampaignPreviewRequest
            {
                Segment = CampaignSegment,
                Language = CampaignLanguage,
                IncludeImages = CampaignIncludeImages,
                Note = CampaignNote,
                MaxRecipients = 100,
                PartIds = selectedParts,
                UsedCarIds = selectedCars
            };
        }

        private async Task RefreshSelectedThreadAsync()
        {
            if (SelectedConversation == null)
            {
                await LoadConversationsAsync();
                return;
            }

            await LoadMessagesAsync(SelectedConversation);
        }

        private async Task LoadMessagesAsync(WhatsAppConversationItemViewModel conversation)
        {
            IsLoading = true;
            Status = $"Loading {conversation.RecipientName}...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var route = $"api/communications/messages?phone={Uri.EscapeDataString(conversation.RecipientPhone)}&take=250";
                var rows = await _crudApi.GetAllAsync<CommunicationConversationMessageDto>(route);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (SelectedConversation == null
                        || !string.Equals(SelectedConversation.RecipientPhone, conversation.RecipientPhone, StringComparison.Ordinal))
                    {
                        return;
                    }

                    Messages.Clear();
                    foreach (var item in rows.Select(dto => new WhatsAppMessageItemViewModel(dto)))
                    {
                        Messages.Add(item);
                    }
                });

                Status = $"Thread loaded. {Messages.Count:N0} message(s).";
                StatusBrush = Brushes.LightGreen;
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            catch (Exception ex)
            {
                Status = $"Could not load thread: {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SendMessageAsync()
        {
            var body = ComposeText.Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                Status = "Type a message first.";
                StatusBrush = Brushes.LightGoldenrodYellow;
                return;
            }

            var phone = (SelectedConversation?.RecipientPhone ?? ManualPhone).Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                Status = "Enter a WhatsApp phone number.";
                StatusBrush = Brushes.LightGoldenrodYellow;
                return;
            }

            var name = (SelectedConversation?.RecipientName ?? ManualName).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Customer";
            }

            IsLoading = true;
            Status = "Sending WhatsApp message...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var response = await _crudApi.PostAsync<SendBusinessMessageResponse>(
                    "api/communications/send",
                    new SendBusinessMessageRequest
                    {
                        Channel = CommunicationChannel.WhatsApp,
                        TemplateKey = CommunicationTemplateKey.FreeText,
                        RecipientKind = CommunicationRecipientKind.Manual,
                        RecipientPhoneOverride = phone,
                        RecipientNameOverride = name,
                        MessageBody = body
                    });

                ComposeText = string.Empty;
                ManualName = response.RecipientName;
                ManualPhone = response.RecipientPhone;
                await LoadConversationsAsync(response.RecipientPhone);
                Status = $"WhatsApp message {response.Status.ToLowerInvariant()}.";
                StatusBrush = string.Equals(response.Status, "Failed", StringComparison.OrdinalIgnoreCase)
                    ? Brushes.OrangeRed
                    : Brushes.LightGreen;
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            catch (Exception ex)
            {
                Status = $"Could not send message: {ex.Message}";
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void StartManualConversation()
        {
            if (string.IsNullOrWhiteSpace(ManualPhone))
            {
                Status = "Enter a phone number first.";
                StatusBrush = Brushes.LightGoldenrodYellow;
                return;
            }

            SelectedConversation = null;
            Messages.Clear();
            Status = "Ready to send a new WhatsApp message.";
            StatusBrush = Brushes.LightGreen;
            OnPropertyChanged(nameof(ActiveRecipientTitle));
            OnPropertyChanged(nameof(ActiveRecipientSubtitle));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void HandleBackgroundException(Exception ex)
        {
            Status = $"Background task failed: {ex.Message}";
            StatusBrush = Brushes.OrangeRed;
        }
    }

    public sealed class CampaignOptionViewModel
    {
        public CampaignOptionViewModel(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }

    public sealed class WhatsAppCampaignAssetItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public WhatsAppCampaignAssetItemViewModel(WhatsAppCampaignAssetDto dto)
        {
            AssetType = dto.AssetType;
            Id = dto.Id;
            Title = dto.Title;
            Subtitle = dto.Subtitle;
            Price = dto.Price;
            Currency = dto.Currency;
            ImageCount = dto.ImageCount;
            _isSelected = dto.IsSelected;
        }

        public string AssetType { get; }
        public int Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public decimal? Price { get; }
        public string? Currency { get; }
        public int ImageCount { get; }
        public string AssetKey => $"{AssetType}:{Id}";
        public string TypeBadge => string.Equals(AssetType, "Car", StringComparison.OrdinalIgnoreCase) ? "CAR" : "PART";
        public string PriceText => Price.HasValue ? $"{(string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency)} {Price.Value:N2}" : string.Empty;
        public string ImageText => ImageCount <= 0 ? string.Empty : $"{ImageCount:N0} image(s)";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
