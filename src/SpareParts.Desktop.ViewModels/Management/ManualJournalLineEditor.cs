using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ManualJournalLineEditor : INotifyPropertyChanged
    {
        private int _accountId;
        private decimal _debit;
        private decimal _credit;

        public int AccountId
        {
            get => _accountId;
            set
            {
                if (_accountId == value) return;
                _accountId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));
            }
        }

        public decimal Debit
        {
            get => _debit;
            set
            {
                if (_debit == value) return;
                _debit = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Debit)));
            }
        }

        public decimal Credit
        {
            get => _credit;
            set
            {
                if (_credit == value) return;
                _credit = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Credit)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
