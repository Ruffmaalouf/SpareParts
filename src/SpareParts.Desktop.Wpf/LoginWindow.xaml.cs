using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;
        private readonly IApiClient _apiClient;

        public LoginWindow(IApiClient? apiClient = null, LoginViewModel? vm = null)
        {
            InitializeComponent();
            _apiClient = apiClient ?? new ApiClient();
            _vm = vm ?? new LoginViewModel(_apiClient);
            DataContext = _vm;

            _vm.LoginSucceeded += _ =>
            {
                new MainWindow(new InvoiceTabsViewModel(_apiClient)).Show();
                Close();
            };

            Loaded += (_, _) => UsernameBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
            => _vm.LoginCommand.Execute(PasswordBox.Password);

        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _vm.LoginCommand.Execute(PasswordBox.Password);
        }

        // X button: borderless window — Close() alone won't kill the process.
        // Application.Current.Shutdown() is the correct call.
        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
