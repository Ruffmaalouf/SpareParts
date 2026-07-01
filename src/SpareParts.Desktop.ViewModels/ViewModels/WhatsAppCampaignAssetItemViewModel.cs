using SpareParts.Domain.Communications;
using System;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
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
