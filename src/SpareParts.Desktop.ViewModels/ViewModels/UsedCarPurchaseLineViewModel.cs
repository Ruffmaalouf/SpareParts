using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class UsedCarPurchaseLineViewModel : INotifyPropertyChanged
    {
        private int? _accountId;

        public int SortOrder { get; set; }
        public string RoleKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal RateToBase { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal CounterAmount { get; set; }

        public int? AccountId
        {
            get => _accountId;
            set
            {
                if (_accountId == value) return;
                _accountId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
