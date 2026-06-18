namespace SpareParts.Domain.Sales
{
    /// <summary>One month of aggregated sales revenue, cost and profit.</summary>
    public class SalesProfitHistoryPointDto
    {
        /// <summary>Calendar month in "yyyy-MM" format.</summary>
        public string Period { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit => decimal.Round(Revenue - Cost, 4, MidpointRounding.AwayFromZero);
        public decimal MarginPercent => Revenue > 0
            ? decimal.Round(Profit / Revenue * 100, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    /// <summary>A single sale line, used to build <see cref="SalesProfitHistoryPointDto"/> buckets.</summary>
    public record SalesProfitHistoryRawLine(int TransactionId, DateTime TransactionDate, decimal Quantity, decimal UnitPrice, decimal CostPrice);

    /// <summary>Groups raw sale lines into a contiguous run of monthly profit/margin buckets.</summary>
    public static class SalesProfitHistoryBuilder
    {
        public static List<SalesProfitHistoryPointDto> Build(IEnumerable<SalesProfitHistoryRawLine> lines, int months, DateTime asOf)
        {
            if (months <= 0)
            {
                months = 1;
            }

            var end = new DateTime(asOf.Year, asOf.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var start = end.AddMonths(-(months - 1));

            var buckets = new Dictionary<string, (decimal Revenue, decimal Cost, HashSet<int> Invoices)>();
            for (var i = 0; i < months; i++)
            {
                var period = start.AddMonths(i).ToString("yyyy-MM");
                buckets[period] = (0m, 0m, new HashSet<int>());
            }

            foreach (var line in lines)
            {
                var period = new DateTime(line.TransactionDate.Year, line.TransactionDate.Month, 1).ToString("yyyy-MM");
                if (!buckets.TryGetValue(period, out var bucket))
                {
                    continue;
                }

                bucket.Revenue += line.Quantity * line.UnitPrice;
                bucket.Cost += line.Quantity * line.CostPrice;
                bucket.Invoices.Add(line.TransactionId);
                buckets[period] = bucket;
            }

            return buckets
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new SalesProfitHistoryPointDto
                {
                    Period = kv.Key,
                    Revenue = decimal.Round(kv.Value.Revenue, 4, MidpointRounding.AwayFromZero),
                    Cost = decimal.Round(kv.Value.Cost, 4, MidpointRounding.AwayFromZero),
                    InvoiceCount = kv.Value.Invoices.Count
                })
                .ToList();
        }
    }
}
