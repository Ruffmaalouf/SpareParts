using SpareParts.Desktop.Wpf.Pricing;
using SpareParts.Domain.Inventory;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class StockSnapshotViewModel
    {
        public int PartId { get; set; }
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public int Available { get; set; }
        public int MinStock { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int WaitingCustomers { get; set; }
        public string PricingCoachBadge { get; set; } = string.Empty;
        public string PricingCoachMessage { get; set; } = string.Empty;
        public string Status => OnHand <= MinStock ? "Low Stock" : "Healthy";

        public static StockSnapshotViewModel FromPart(PartDto part, int waitingCustomers)
        {
            var coach = SmartPricingCoach.Evaluate(part, waitingCustomers);
            return new StockSnapshotViewModel
            {
                PartId = part.Id,
                PartCode = part.InternalCode,
                PartName = part.Name,
                Warehouse = part.UsedCarId is int usedCarId ? $"Used car #{usedCarId}" : "Shelf stock",
                OnHand = part.StockQuantity,
                Available = part.AvailableQuantity,
                MinStock = part.MinStock,
                SalePrice = part.SalePrice,
                Currency = part.Currency,
                WaitingCustomers = waitingCustomers,
                PricingCoachBadge = coach.Badge,
                PricingCoachMessage = coach.Message
            };
        }
    }
}
