using SpareParts.Domain.Pricing;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class BillingPackageViewModel : INotifyPropertyChanged
    {
        private string _priceLabel = string.Empty;
        private string _actionKind = "current";
        private string _actionLabel = "Current plan";
        private bool _isCurrent;
        private bool _showTrialButton;
        private bool _isBusy;

        public BillingPackageViewModel(PricingPackageDto package)
        {
            Package = package;
        }

        public PricingPackageDto Package { get; }
        public string Code => Package.Code;
        public string Name => Package.Name;
        public string Description => Package.Description;
        public int SortOrder => Package.SortOrder;
        public bool IsCustomPricing => Package.IsCustomPricing;

        public string PriceLabel
        {
            get => _priceLabel;
            set { if (_priceLabel == value) return; _priceLabel = value; OnPropertyChanged(nameof(PriceLabel)); }
        }

        public string ActionKind
        {
            get => _actionKind;
            set
            {
                if (_actionKind == value) return;
                _actionKind = value;
                OnPropertyChanged(nameof(ActionKind));
                OnPropertyChanged(nameof(ShowContactButton));
                OnPropertyChanged(nameof(ShowUpgradeButton));
                OnPropertyChanged(nameof(ShowDowngradeButton));
            }
        }

        public string ActionLabel
        {
            get => _actionLabel;
            set { if (_actionLabel == value) return; _actionLabel = value; OnPropertyChanged(nameof(ActionLabel)); }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set { if (_isCurrent == value) return; _isCurrent = value; OnPropertyChanged(nameof(IsCurrent)); }
        }

        public bool ShowTrialButton
        {
            get => _showTrialButton;
            set { if (_showTrialButton == value) return; _showTrialButton = value; OnPropertyChanged(nameof(ShowTrialButton)); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        public bool ShowContactButton => ActionKind == "contact";
        public bool ShowUpgradeButton => ActionKind == "upgrade";
        public bool ShowDowngradeButton => ActionKind == "downgrade";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
