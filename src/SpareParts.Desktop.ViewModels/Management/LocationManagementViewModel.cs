using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class LocationManagementViewModel
    {
        public ObservableCollection<LocationDto> Locations { get; } = new();
        public LocationDto? SelectedLocation { get; set; }

        public string NewLocationName { get; set; } = string.Empty;
        public decimal NewLocationShippingFees { get; set; }
        public string NewLocationShippingFeesCurrencyCode { get; set; } = "USD";

        public void PopulateForm(LocationDto location)
        {
            NewLocationName = location.Name;
            NewLocationShippingFees = location.ShippingFees;
            NewLocationShippingFeesCurrencyCode = string.IsNullOrWhiteSpace(location.ShippingFeesCurrencyCode)
                ? "USD"
                : location.ShippingFeesCurrencyCode.Trim().ToUpperInvariant();
        }

        public void ClearForm(string defaultCurrencyCode)
        {
            NewLocationName = string.Empty;
            NewLocationShippingFees = 0m;
            NewLocationShippingFeesCurrencyCode = string.IsNullOrWhiteSpace(defaultCurrencyCode)
                ? "USD"
                : defaultCurrencyCode.Trim().ToUpperInvariant();
            SelectedLocation = null;
        }
    }
}
