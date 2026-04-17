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
        public string NewPartAveragePrice { get; set; } = string.Empty;
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
            NewPartOEM = p.OEMNumber ?? string.Empty;
            NewPartCategoryId = p.CategoryId;
            NewPartBrandId = p.BrandId;
            NewPartCostPrice = p.CostPrice;
            NewPartSalePrice = p.SalePrice;
            NewPartAveragePrice = p.AveragePrice?.ToString("0.##") ?? string.Empty;
            NewPartCurrency = p.Currency;
            NewPartMinStock = p.MinStock;
            NewPartNotes = p.Notes ?? string.Empty;
        }

        public void ClearForm(string defaultCurrencyCode = "USD")
        {
            NewPartCode = NewPartName = NewPartOEM = NewPartAveragePrice = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            NewPartCurrency = defaultCurrencyCode;
            NewPartMinStock = 0;
            NewPartCategoryId = 1;
            NewPartBrandId = null;
            SelectedPart = null;
        }
    }
}
