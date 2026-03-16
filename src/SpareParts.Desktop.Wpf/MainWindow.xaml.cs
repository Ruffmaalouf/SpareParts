using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new InvoiceTabsViewModel();
        }
    }
}
