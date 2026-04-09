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
            NewSupplierPhone = s.Phone ?? string.Empty;
            NewSupplierEmail = s.Email ?? string.Empty;
            NewSupplierAddress = s.Address ?? string.Empty;
            NewSupplierTax = s.TaxNumber ?? string.Empty;
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
