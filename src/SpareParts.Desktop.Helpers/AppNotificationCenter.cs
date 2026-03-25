using System.Collections.ObjectModel;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public sealed class AppNotificationCenter
    {
        public static AppNotificationCenter Instance { get; } = new();
        public ObservableCollection<StatusMessage> Messages { get; } = new();

        private AppNotificationCenter() { }

        public void Publish(string text, bool isSuccess)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            void AddMessage()
            {
                Messages.Insert(0, new StatusMessage { Text = text, IsSuccess = isSuccess });
                const int maxMessages = 12;
                while (Messages.Count > maxMessages)
                {
                    Messages.RemoveAt(Messages.Count - 1);
                }
            }

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                AddMessage();
                return;
            }

            Application.Current?.Dispatcher?.Invoke(AddMessage);
        }
    }
}
