using SpareParts.Domain.BusinessPartners;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class SupplierManagementViewModel
    {
        public ObservableCollection<SupplierDto> Suppliers { get; } = new();
        public SupplierDto? SelectedSupplier { get; set; }

        public string NewSupplierName { get; set; } = string.Empty;
        public string NewSupplierPhone { get; set; } = string.Empty;
        public string NewSupplierEmail { get; set; } = string.Empty;
        public string NewSupplierAddress { get; set; } = string.Empty;
        public string NewSupplierTax { get; set; } = string.Empty;
        public decimal NewSupplierBalance { get; set; }

        public void PopulateForm(SupplierDto s)
        {
            NewSupplierName = s.Name;
            NewSupplierPhone = s.Phone;
            NewSupplierEmail = s.Email;
            NewSupplierAddress = s.Address;
            NewSupplierTax = s.TaxNumber;
            NewSupplierBalance = s.OpeningBalance;
        }

        public void ClearForm()
        {
            NewSupplierName = NewSupplierPhone = NewSupplierEmail = NewSupplierAddress = NewSupplierTax = string.Empty;
            NewSupplierBalance = 0;
            SelectedSupplier = null;
        }
    }
}
