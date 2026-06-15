using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal readonly record struct SlowMovingPartFilter(string BrandTerm, decimal? MinSalePrice, DateTime? PurchasedBefore)
    {
        public bool HasFilters => !string.IsNullOrWhiteSpace(BrandTerm) || MinSalePrice != null || PurchasedBefore != null;

        public string Summary
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(BrandTerm))
                {
                    parts.Add($"brand {BrandTerm}");
                }

                if (MinSalePrice != null)
                {
                    parts.Add($"sale price over {MinSalePrice.Value.ToString("N2", CultureInfo.CurrentCulture)}");
                }

                if (PurchasedBefore != null)
                {
                    parts.Add($"received before {PurchasedBefore.Value:yyyy-MM-dd}");
                }

                return parts.Count == 0 ? "no filters" : string.Join(", ", parts);
            }
        }
    }
}
