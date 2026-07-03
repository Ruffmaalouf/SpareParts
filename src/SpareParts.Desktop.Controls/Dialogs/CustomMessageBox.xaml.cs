using System.Linq;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class CustomMessageBox : Window
    {
        /// <summary>True once the user has confirmed a Yes/No dialog created via <see cref="ShowConfirmation"/>.</summary>
        public bool Confirmed { get; private set; }

        public CustomMessageBox(string title, string message, string type = "Info", bool showCancelButton = false)
        {
            InitializeComponent();
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;
            SetStyle(type);

            if (showCancelButton)
            {
                OkButton.Content = "Yes";
                CancelButton.Visibility = Visibility.Visible;
            }
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
            Confirmed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            this.Close();
        }

        // Static method to show the modern dialog easily
        public static void Show(string message, string title = "System Notification", string type = "Info")
        {
            ShowDialogInternal(title, message, type, showCancelButton: false);
        }

        /// <summary>
        /// Shows a themed Yes/No confirmation dialog and returns true if the user confirmed ("Yes"),
        /// false otherwise (including closing the dialog without choosing).
        /// </summary>
        public static bool ShowConfirmation(string message, string title = "Confirm", string type = "Warning")
        {
            var dialog = ShowDialogInternal(title, message, type, showCancelButton: true);
            return dialog.Confirmed;
        }

        private static CustomMessageBox ShowDialogInternal(string title, string message, string type, bool showCancelButton)
        {
            var dialog = new CustomMessageBox(title, message, type, showCancelButton);
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
            return dialog;
        }
    }
}
