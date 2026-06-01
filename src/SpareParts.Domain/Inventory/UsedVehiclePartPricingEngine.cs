namespace SpareParts.Domain.Inventory
{
    public static class UsedVehiclePartPricingEngine
    {
        public const decimal DefaultExpectedSellThroughRate = 0.80m;
        public const decimal FastSaleMarkupRate = 0.10m;
        public const decimal WholesaleMarkupRate = 0.20m;

        public static UsedVehiclePartPricingResult Calculate(
            decimal vehicleTotalCost,
            decimal expectedSellThroughRate,
            IEnumerable<UsedVehiclePartPricingInput> parts)
        {
            var normalizedSellThroughRate = NormalizeExpectedSellThroughRate(expectedSellThroughRate);
            var effectiveVehicleCost = RoundMoney(vehicleTotalCost / normalizedSellThroughRate);
            var rows = parts
                .Select(part =>
                {
                    var rateToBase = NormalizeRateToBase(part.RateToBase);
                    var expectedMarketPrice = ResolveExpectedMarketPrice(part);
                    return new PricingSeed(
                        part,
                        expectedMarketPrice,
                        rateToBase,
                        expectedMarketPrice * rateToBase);
                })
                .ToList();
            var totalExpectedRevenue = rows.Sum(row => row.ExpectedMarketPriceBase);

            if (vehicleTotalCost <= 0m || totalExpectedRevenue <= 0m)
            {
                return new UsedVehiclePartPricingResult(
                    RoundMoney(vehicleTotalCost),
                    effectiveVehicleCost,
                    normalizedSellThroughRate,
                    RoundMoney(totalExpectedRevenue),
                    rows.Select(row => new UsedVehiclePartPricingRow(
                            row.Input.PartId,
                            RoundMoney(row.ExpectedMarketPrice),
                            0m,
                            0m,
                            0m,
                            0m,
                            0m,
                            RoundMoney(row.ExpectedMarketPrice),
                            row.ExpectedMarketPrice > 0m ? "NeedsVehicleCost" : "NeedsExpectedPrice"))
                        .ToList());
            }

            var pricedRows = rows.Select(row =>
            {
                var allocationRatio = row.ExpectedMarketPriceBase / totalExpectedRevenue;
                var allocatedCostBase = effectiveVehicleCost * allocationRatio;
                var allocatedCost = RoundMoney(allocatedCostBase / row.RateToBase);
                var minimumPrice = allocatedCost;
                var fastSalePrice = RoundMoney(allocatedCost * (1m + FastSaleMarkupRate));
                var wholesalePrice = RoundMoney(allocatedCost * (1m + WholesaleMarkupRate));
                var recommendedPrice = RoundMoney(Math.Max(row.ExpectedMarketPrice, wholesalePrice));

                return new UsedVehiclePartPricingRow(
                    row.Input.PartId,
                    RoundMoney(row.ExpectedMarketPrice),
                    RoundPercent(allocationRatio * 100m),
                    allocatedCost,
                    minimumPrice,
                    fastSalePrice,
                    wholesalePrice,
                    recommendedPrice,
                    row.ExpectedMarketPrice > 0m ? "Allocated" : "NeedsExpectedPrice");
            }).ToList();

            return new UsedVehiclePartPricingResult(
                RoundMoney(vehicleTotalCost),
                effectiveVehicleCost,
                normalizedSellThroughRate,
                RoundMoney(totalExpectedRevenue),
                pricedRows);
        }

        public static decimal NormalizeExpectedSellThroughRate(decimal expectedSellThroughRate)
            => expectedSellThroughRate is > 0m and <= 1m ? expectedSellThroughRate : DefaultExpectedSellThroughRate;

        public static decimal NormalizeRateToBase(decimal rateToBase)
            => rateToBase > 0m ? rateToBase : 1m;

        private static decimal ResolveExpectedMarketPrice(UsedVehiclePartPricingInput part)
        {
            if (part.EstimatedMarketPrice is > 0m)
            {
                return part.EstimatedMarketPrice.Value;
            }

            if (part.AveragePrice is > 0m)
            {
                return part.AveragePrice.Value;
            }

            return part.SalePrice > 0m ? part.SalePrice : 0m;
        }

        private static decimal RoundMoney(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static decimal RoundPercent(decimal value)
            => decimal.Round(value, 4, MidpointRounding.AwayFromZero);

        private sealed record PricingSeed(
            UsedVehiclePartPricingInput Input,
            decimal ExpectedMarketPrice,
            decimal RateToBase,
            decimal ExpectedMarketPriceBase);
    }

    public sealed record UsedVehiclePartPricingInput(
        int PartId,
        decimal? EstimatedMarketPrice,
        decimal? AveragePrice,
        decimal SalePrice,
        decimal RateToBase = 1m);

    public sealed record UsedVehiclePartPricingResult(
        decimal VehicleTotalCost,
        decimal EffectiveVehicleCost,
        decimal ExpectedSellThroughRate,
        decimal TotalExpectedRevenue,
        IReadOnlyList<UsedVehiclePartPricingRow> Rows);

    public sealed record UsedVehiclePartPricingRow(
        int PartId,
        decimal ExpectedMarketPrice,
        decimal CostAllocationPercent,
        decimal AllocatedCost,
        decimal MinimumSellPrice,
        decimal FastSalePrice,
        decimal WholesalePrice,
        decimal RecommendedPrice,
        string PricingStatus);
}
