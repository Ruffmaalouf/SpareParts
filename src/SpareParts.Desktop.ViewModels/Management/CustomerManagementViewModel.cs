using SpareParts.Domain.BusinessPartners;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class CustomerManagementViewModel
    {
        public ObservableCollection<CustomerDto> Customers { get; } = new();
        public CustomerDto? SelectedCustomer { get; set; }

        public string NewCustomerName { get; set; } = string.Empty;
        public string NewCustomerPhone { get; set; } = string.Empty;
        public string NewCustomerEmail { get; set; } = string.Empty;
        public string NewCustomerAddress { get; set; } = string.Empty;
        public string NewCustomerTax { get; set; } = string.Empty;
        public decimal NewCustomerBalance { get; set; }

        public void PopulateForm(CustomerDto c)
        {
            NewCustomerName = c.Name;
            NewCustomerPhone = c.Phone;
            NewCustomerEmail = c.Email;
            NewCustomerAddress = c.Address;
            NewCustomerTax = c.TaxNumber;
            NewCustomerBalance = c.OpeningBalance;
        }

        public void ClearForm()
        {
            NewCustomerName = NewCustomerPhone = NewCustomerEmail = NewCustomerAddress = NewCustomerTax = string.Empty;
            NewCustomerBalance = 0;
            SelectedCustomer = null;
        }
    }
}
