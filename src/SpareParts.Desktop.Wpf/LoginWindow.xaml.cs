using SpareParts.Desktop.Wpf.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;
        private readonly MainWindow _mainWindow;

        public LoginWindow(LoginViewModel vm, MainWindow mainWindow)
        {
            InitializeComponent();
            _vm = vm;
            _mainWindow = mainWindow;
            DataContext = _vm;

            _vm.LoginSucceeded += _ =>
            {
                _mainWindow.Show();
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
