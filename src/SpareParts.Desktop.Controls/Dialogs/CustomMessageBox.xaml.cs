using System.Linq;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string title, string message, string type = "Info")
        {
            InitializeComponent();
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;
            SetStyle(type);
        }

        private void SetStyle(string type)
        {
            switch (type)
            {
                case "Success":
                    Title = "Confirmation";
                    IconText.Text = "✓";
                    IconText.Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("AccentBrush"); // Re-using accent for success
                    break;
                case "Error":
                    Title = "Critical Error";
                    IconText.Text = "X";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)); // Warning Red
                    break;
                case "Warning":
                    Title = "Warning";
                    IconText.Text = "⚠";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)); // Amber
                    break;
                case "Info":
                default:
                    Title = "Information";
                    IconText.Text = "i";
                    IconText.Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush");
                    break;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Static method to show the modern dialog easily
        public static void Show(string message, string title = "System Notification", string type = "Info")
        {
            var dialog = new CustomMessageBox(title, message, type);
            var owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive)
                ?? Application.Current?.MainWindow;

            if (owner != null && owner != dialog)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dialog.ShowDialog();
        }
    }
}
