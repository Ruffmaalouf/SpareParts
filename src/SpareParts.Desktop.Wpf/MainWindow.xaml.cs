using SpareParts.Desktop.Wpf.ViewModels;
using SpareParts.Domain.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow(InvoiceTabsViewModel? vm = null)
        {
            InitializeComponent();
            DataContext = vm ?? new InvoiceTabsViewModel();
        }

        private async void InvoiceSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not InvoiceTabsViewModel vm)
            {
                return;
            }

            if (sender is not DataGrid grid || grid.SelectedItem is not SalesInvoiceLookupDto selected)
            {
                return;
            }

            await vm.OpenInvoiceFromSearchAsync(selected);
        }
    }
}
