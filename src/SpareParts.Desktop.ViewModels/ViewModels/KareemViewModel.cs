using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class KareemViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private bool _isBusy;
        private string _status = string.Empty;
        private Brush _statusBrush = Brushes.LightGray;
        private string _message = string.Empty;
        private string _language = "en";

        public KareemViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            SendCommand = new RelayCommand(_ => SendAsync().SafeFireAndForget(HandleException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<KareemChatMessage> History { get; } = new();
        public ICommand SendCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

        public string Language
        {
            get => _language;
            set { _language = value; OnPropertyChanged(nameof(Language)); }
        }

        public async Task SendAsync()
        {
            var text = Message.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var priorHistory = History
                .Select(m => new { role = m.IsUser ? 1 : 2, content = m.Text })
                .ToList();

            History.Add(new KareemChatMessage { IsUser = true, Text = text });
            Message = string.Empty;
            IsBusy = true;
            Status = "Kareem is typing...";
            try
            {
                var payload = new { message = text, language = Language, history = priorHistory };
                var result = await _crudApi.PostAsync<JsonElement>("/api/kareem/chat", payload);

                var reply = result.ValueKind == JsonValueKind.Object &&
                    result.TryGetProperty("reply", out var replyProp) &&
                    replyProp.ValueKind == JsonValueKind.String
                        ? replyProp.GetString() ?? string.Empty
                        : string.Empty;

                History.Add(new KareemChatMessage { IsUser = false, Text = reply });
                Status = string.Empty;
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleException(Exception ex)
        {
            Status = ex.Message;
            StatusBrush = Brushes.OrangeRed;
            IsBusy = false;
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
