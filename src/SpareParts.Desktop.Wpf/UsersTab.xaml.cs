using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    public partial class UsersTab : UserControl
    {
        public UsersTab()
        {
            InitializeComponent();
        }

        // PasswordBox can't bind directly — relay password changes to ViewModel
        private void PasswordInput_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UsersViewModel vm)
                vm.FormPassword = PasswordInput.Password;
        }
    }
}
