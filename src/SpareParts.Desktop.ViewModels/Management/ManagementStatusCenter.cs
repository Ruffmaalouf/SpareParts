using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.Management
{
    public class ManagementStatusCenter : INotifyPropertyChanged
    {
        private string _status = string.Empty;
        private Brush _statusBrush = Brushes.DodgerBlue;

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value) return;
                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public ObservableCollection<StatusMessage> StatusMessages { get; } = new();

        public void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.MediumSeaGreen : Brushes.IndianRed;
            StatusMessages.Insert(0, new StatusMessage { Text = message, IsSuccess = isSuccess });
            AppNotificationCenter.Instance.Publish(message, isSuccess);

            const int maxMessages = 8;
            while (StatusMessages.Count > maxMessages)
            {
                StatusMessages.RemoveAt(StatusMessages.Count - 1);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
