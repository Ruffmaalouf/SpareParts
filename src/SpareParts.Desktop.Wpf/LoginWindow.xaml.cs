using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;

        public LoginWindow()
        {
            InitializeComponent();
            _vm = new LoginViewModel();
            DataContext = _vm;

            _vm.LoginSucceeded += _ =>
            {
                new MainWindow().Show();
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
