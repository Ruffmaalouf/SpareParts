using SpareParts.Domain.Inventory;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class DeadStockItemRow
    {
        private DeadStockItemRow()
        {
        }

        public int PartId { get; private init; }
        public string InternalCode { get; private init; } = string.Empty;
        public string DisplayCode { get; private init; } = string.Empty;
        public string PartName { get; private init; } = string.Empty;
        public string OemDisplay { get; private init; } = string.Empty;
        public string Currency { get; private init; } = "USD";
        public decimal SalePrice { get; private init; }
        public decimal UnitCost { get; private init; }
        public decimal OnHand { get; private init; }
        public decimal AvailableQuantity { get; private init; }
        public decimal StockValue { get; private init; }
        public int DormantDays { get; private init; }
        public string PrimaryAction { get; private init; } = string.Empty;
        public string SearchText { get; private init; } = string.Empty;
        public string DormancyLabel => $"{DormantDays:N0} days dormant";
        public string StockLabel => $"{OnHand:N0} on hand / {AvailableQuantity:N0} available";
        public string SalePriceLabel => FormatMoney(SalePrice, Currency);
        public string CostLabel => FormatMoney(UnitCost, Currency);
        public string StockValueLabel => FormatMoney(StockValue, Currency);
        public string LastSoldLabel { get; private init; } = "never";
        public string LastReceivedLabel { get; private init; } = "never";
        public ObservableCollection<DeadStockActionRow> SuggestedActions { get; } = new();

        public static DeadStockItemRow From(DeadStockItemDto source)
        {
            var row = new DeadStockItemRow
            {
                PartId = source.PartId,
                InternalCode = source.InternalCode ?? string.Empty,
                DisplayCode = ResolveDisplayCode(source.InternalCode, source.PartId),
                PartName = source.PartName ?? string.Empty,
                OemDisplay = FormatOem(source.OemNumber),
                Currency = NormalizeCurrency(source.Currency),
                SalePrice = source.SalePrice,
                UnitCost = source.UnitCost,
                OnHand = source.OnHand,
                AvailableQuantity = source.AvailableQuantity,
                StockValue = source.StockValue,
                DormantDays = source.DormantDays,
                PrimaryAction = source.PrimaryAction,
                LastSoldLabel = FormatDate(source.LastSoldAt),
                LastReceivedLabel = FormatDate(source.LastReceivedAt),
                SearchText = Normalize($"{source.InternalCode} {source.PartName} {source.OemNumber} {source.PrimaryAction}")
            };

            foreach (var action in source.SuggestedActions.Select(DeadStockActionRow.From))
            {
                row.SuggestedActions.Add(action);
            }

            return row;
        }

        internal static string FormatMoney(decimal amount, string? currency)
            => $"{amount:N2} {NormalizeCurrency(currency)}";

        internal static string NormalizeCurrency(string? currency)
            => string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();

        private static string ResolveDisplayCode(string? code, int id)
            => string.IsNullOrWhiteSpace(code) ? $"PART-{id}" : code.Trim();

        private static string FormatOem(string? oem)
            => string.IsNullOrWhiteSpace(oem) ? "not set" : oem.Trim();

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture) : "never";

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Trim().Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ').ToArray();
            return string.Join(" ", new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
    }
}
