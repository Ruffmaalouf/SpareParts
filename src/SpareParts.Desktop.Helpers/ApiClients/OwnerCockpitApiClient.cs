using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.OwnerCockpit;
using System;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class OwnerCockpitApiClient : FeatureApiClientBase, IOwnerCockpitApiClient
    {
        public OwnerCockpitApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<OwnerCockpitDashboardDto> GetDashboardAsync(DateTime? businessDate = null)
        {
            var resource = "api/owner-cockpit";
            if (businessDate != null)
            {
                resource = $"{resource}?businessDate={Uri.EscapeDataString(businessDate.Value.ToString("yyyy-MM-dd"))}";
            }

            return RetrieveOneAsync<OwnerCockpitDashboardDto>(resource, "Owner cockpit dashboard was empty.");
        }
    }
}
