using SpareParts.Desktop.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new InvoiceTabsViewModel();
        }

        private void InvoiceSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not InvoiceTabsViewModel vm)
            {
                return;
            }

            if (sender is not DataGrid grid || grid.SelectedItem is not InvoiceTabViewModel selected)
            {
                return;
            }

            vm.OpenInvoiceFromSearch(selected);
        }
    }
}
