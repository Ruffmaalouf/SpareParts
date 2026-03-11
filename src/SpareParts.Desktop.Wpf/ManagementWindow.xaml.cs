using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class ManagementWindow : Window
    {
        public ManagementWindow()
        {
            InitializeComponent();
            DataContext = new ManagementViewModel();
        }
    }
}
