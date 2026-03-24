using System.Reflection;
using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    public partial class UsersTab : UserControl
    {
        public UsersTab()
        {
            InitializeComponent();
        }

        // PasswordBox cannot bind directly — relay changes to DataContext.FormPassword if available
        private void PasswordInput_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                return;
            }

            PropertyInfo? formPasswordProperty = DataContext.GetType().GetProperty("FormPassword");
            if (formPasswordProperty?.CanWrite == true)
            {
                formPasswordProperty.SetValue(DataContext, PasswordInput.Password);
            }
        }
    }
}
