using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartCompatibilityPartRow
    {
        private PartCompatibilityPartRow(
            PartDto source,
            UsedCarDto? car,
            string normalizedOem,
            string normalizedName,
            IReadOnlyList<string> nameTokens,
            string searchText)
        {
            Id = source.Id;
            InternalCode = source.InternalCode ?? string.Empty;
            Name = source.Name ?? string.Empty;
            OemNumber = source.OEMNumber ?? string.Empty;
            CategoryId = source.CategoryId;
            UsedCarId = source.UsedCarId;
            StockQuantity = source.StockQuantity;
            AvailableQuantity = source.AvailableQuantity;
            SalePrice = source.SalePrice;
            Currency = source.Currency ?? "USD";
            IsActive = source.IsActive;
            VehicleName = car?.Car ?? string.Empty;
            ModelYear = car?.ModelYear ?? 0;
            NormalizedOem = normalizedOem;
            NormalizedName = normalizedName;
            NameTokens = nameTokens;
            SearchText = searchText;
        }

        public int Id { get; }
        public string InternalCode { get; }
        public string Name { get; }
        public string OemNumber { get; }
        public int CategoryId { get; }
        public int? UsedCarId { get; }
        public int StockQuantity { get; }
        public int AvailableQuantity { get; }
        public decimal SalePrice { get; }
        public string Currency { get; }
        public bool IsActive { get; }
        public string VehicleName { get; }
        public int ModelYear { get; }
        public string NormalizedOem { get; }
        public string NormalizedName { get; }
        public IReadOnlyList<string> NameTokens { get; }
        public string SearchText { get; }
        public bool HasOem => !string.IsNullOrWhiteSpace(NormalizedOem);
        public string DisplayCode => string.IsNullOrWhiteSpace(InternalCode) ? $"PART-{Id}" : InternalCode;
        public string OemDisplay => string.IsNullOrWhiteSpace(OemNumber) ? "not set" : OemNumber;
        public string VehicleLabel => string.IsNullOrWhiteSpace(VehicleName)
            ? "No donor vehicle"
            : ModelYear > 0 ? $"{VehicleName} {ModelYear}" : VehicleName;
        public string StockLabel => AvailableQuantity > 0
            ? $"{AvailableQuantity:N0} available / {StockQuantity:N0} on hand"
            : $"{StockQuantity:N0} on hand";
        public string PriceLabel => $"{SalePrice:N2} {Currency}";
        public string ListSubtitle => $"{DisplayCode} - {VehicleLabel}";

        public static PartCompatibilityPartRow From(PartDto source, UsedCarDto? car)
        {
            var normalizedOem = NormalizeIdentifier(source.OEMNumber);
            var normalizedName = NormalizeSearch(source.Name);
            var tokens = Tokenize(source.Name);
            var vehicleText = car == null ? string.Empty : $"{car.Car} {car.ModelYear}";
            var searchText = NormalizeSearch($"{source.InternalCode} {source.Name} {source.OEMNumber} {vehicleText}");

            return new PartCompatibilityPartRow(source, car, normalizedOem, normalizedName, tokens, searchText);
        }

        private static string NormalizeIdentifier(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        private static string NormalizeSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value
                .Trim()
                .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ')
                .ToArray();

            return string.Join(" ", new string(chars)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private static IReadOnlyList<string> Tokenize(string? value)
        {
            return NormalizeSearch(value)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(token => token.Length > 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
