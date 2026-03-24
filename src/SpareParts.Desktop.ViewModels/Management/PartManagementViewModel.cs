using SpareParts.Domain.Inventory;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class PartManagementViewModel
    {
        public ObservableCollection<PartDto> Parts { get; } = new();

        public string NewPartCode { get; set; } = string.Empty;
        public string NewPartName { get; set; } = string.Empty;
        public string NewPartOEM { get; set; } = string.Empty;
        public decimal NewPartCostPrice { get; set; }
        public decimal NewPartSalePrice { get; set; }
        public string NewPartCurrency { get; set; } = "USD";
        public int NewPartMinStock { get; set; }
        public int NewPartCategoryId { get; set; } = 1;
        public int? NewPartBrandId { get; set; }
        public string NewPartNotes { get; set; } = string.Empty;
        public PartDto? SelectedPart { get; set; }

        public void PopulateForm(PartDto p)
        {
            NewPartCode = p.InternalCode;
            NewPartName = p.Name;
            NewPartOEM = p.OEMNumber;
            NewPartCategoryId = p.CategoryId;
            NewPartBrandId = p.BrandId;
            NewPartCostPrice = p.CostPrice;
            NewPartSalePrice = p.SalePrice;
            NewPartCurrency = p.Currency;
            NewPartMinStock = p.MinStock;
            NewPartNotes = p.Notes;
        }

        public void ClearForm()
        {
            NewPartCode = NewPartName = NewPartOEM = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            NewPartCurrency = "USD";
            NewPartMinStock = 0;
            NewPartCategoryId = 1;
            NewPartBrandId = null;
            SelectedPart = null;
        }
    }
}
