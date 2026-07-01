using SpareParts.Domain.Inventory;
using System;

namespace SpareParts.Desktop.Wpf
{
    public sealed class UsedCarPartRecommendation
    {
        private UsedCarPartRecommendation()
        {
        }

        public int PartId { get; private init; }

        public string Code { get; private init; } = string.Empty;

        public string Name { get; private init; } = string.Empty;

        public string Currency { get; private init; } = "USD";

        public decimal SalePrice { get; private init; }

        public decimal MinimumSellPrice { get; private init; }

        public decimal RecommendedPrice { get; private init; }

        public int QuantityAvailable { get; private init; }

        public decimal OpportunityValue { get; private init; }

        public decimal MarginValue { get; private init; }

        public bool IsAssigned { get; private init; }

        public decimal Score => OpportunityValue + (MarginValue > 0m ? MarginValue : 0m);

        public string SourceLabel => IsAssigned ? "Linked stock" : "Candidate";

        public string ActionLabel => IsAssigned ? "Plan removal" : "Assign candidate";

        public string QuantityLabel => $"{QuantityAvailable:N0} available";

        public string OpportunityLabel => $"{Currency} {OpportunityValue:N2} opportunity";

        public string MarginLabel => $"{Currency} {MarginValue:N2} margin";

        public string MinimumLabel => $"{Currency} {MinimumSellPrice:N2} min";

        public string RecommendedLabel => $"{Currency} {RecommendedPrice:N2} target";

        public static UsedCarPartRecommendation FromPart(PartDto part, bool isAssigned)
        {
            var quantity = part.AvailableQuantity > 0
                ? part.AvailableQuantity
                : Math.Max(0, part.StockQuantity - part.ReservedQuantity);
            var salePrice = part.RecommendedPrice > 0m ? part.RecommendedPrice : part.SalePrice;
            var costBasis = part.MinimumSellPrice > 0m
                ? part.MinimumSellPrice
                : part.AllocatedCost > 0m
                    ? part.AllocatedCost
                    : part.CostPrice;

            return new UsedCarPartRecommendation
            {
                PartId = part.Id,
                Code = part.InternalCode,
                Name = part.Name,
                Currency = part.Currency,
                SalePrice = salePrice,
                MinimumSellPrice = costBasis,
                RecommendedPrice = salePrice,
                QuantityAvailable = quantity,
                OpportunityValue = RoundMoney(salePrice * quantity),
                MarginValue = RoundMoney((salePrice - costBasis) * quantity),
                IsAssigned = isAssigned
            };
        }

        private static decimal RoundMoney(decimal amount)
            => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
