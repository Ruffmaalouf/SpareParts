using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    internal sealed class PurchaseEditorSnapshot
    {
        public int? PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public int? WarehouseId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal PaidAmount { get; set; }
        public List<PurchaseEditorSnapshotLine> Items { get; set; } = new();
    }
}
