namespace SpareParts.Domain.OwnerCockpit
{
    public static class OwnerCockpitDailyProfitLossCalculator
    {
        public const string RentCategory = "Rent";
        public const string LaborCategory = "Labor";
        public const string OtherCategory = "Other";

        private static readonly string[] RentTerms = { "rent", "lease", "landlord" };
        private static readonly string[] LaborTerms = { "labor", "labour", "salary", "salaries", "wage", "wages", "payroll", "staff", "employee" };

        public static OwnerCockpitDailyProfitLossDto Build(
            DateTime businessDate,
            string currencyCode,
            decimal grossSales,
            decimal grossProfit,
            IEnumerable<OwnerCockpitExpenseBreakdownRowDto>? expenseBreakdown)
        {
            var rows = (expenseBreakdown ?? Enumerable.Empty<OwnerCockpitExpenseBreakdownRowDto>())
                .Select(NormalizeRow)
                .Where(row => row.Amount != 0m || row.EntryCount > 0)
                .ToList();

            var rentExpense = SumCategory(rows, RentCategory);
            var laborExpense = SumCategory(rows, LaborCategory);
            var otherExpense = SumCategory(rows, OtherCategory);
            var totalOperatingExpenses = rentExpense + laborExpense + otherExpense;

            return new OwnerCockpitDailyProfitLossDto
            {
                BusinessDate = businessDate.Date,
                CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant(),
                GrossSales = Round(grossSales),
                CostOfGoodsSold = Round(grossSales - grossProfit),
                GrossProfit = Round(grossProfit),
                RentExpense = Round(rentExpense),
                LaborExpense = Round(laborExpense),
                OtherOperatingExpenses = Round(otherExpense),
                TotalOperatingExpenses = Round(totalOperatingExpenses),
                NetProfitLoss = Round(grossProfit - totalOperatingExpenses),
                ExpenseBreakdown = rows
            };
        }

        public static string ClassifyExpense(string? accountCode, string? accountName, string? description)
        {
            var haystack = $"{accountCode} {accountName} {description}".ToLowerInvariant();
            if (ContainsAny(haystack, RentTerms))
            {
                return RentCategory;
            }

            return ContainsAny(haystack, LaborTerms) ? LaborCategory : OtherCategory;
        }

        private static OwnerCockpitExpenseBreakdownRowDto NormalizeRow(OwnerCockpitExpenseBreakdownRowDto row)
        {
            var category = NormalizeCategory(row.Category);
            return new OwnerCockpitExpenseBreakdownRowDto
            {
                Category = category,
                AccountCode = (row.AccountCode ?? string.Empty).Trim(),
                AccountName = (row.AccountName ?? string.Empty).Trim(),
                Amount = Round(row.Amount),
                EntryCount = row.EntryCount
            };
        }

        private static string NormalizeCategory(string? category)
        {
            if (string.Equals(category, RentCategory, StringComparison.OrdinalIgnoreCase))
            {
                return RentCategory;
            }

            if (string.Equals(category, LaborCategory, StringComparison.OrdinalIgnoreCase))
            {
                return LaborCategory;
            }

            return OtherCategory;
        }

        private static bool ContainsAny(string haystack, IEnumerable<string> terms)
            => terms.Any(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));

        private static decimal SumCategory(IEnumerable<OwnerCockpitExpenseBreakdownRowDto> rows, string category)
            => rows
                .Where(row => string.Equals(row.Category, category, StringComparison.OrdinalIgnoreCase))
                .Sum(row => row.Amount);

        private static decimal Round(decimal value)
            => decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
