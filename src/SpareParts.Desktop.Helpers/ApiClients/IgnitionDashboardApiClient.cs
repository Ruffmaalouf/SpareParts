using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class IgnitionDashboardApiClient : FeatureApiClientBase, IIgnitionDashboardApiClient
    {
        public IgnitionDashboardApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<IgnitionDashboardSummaryDto> GetSummaryAsync(DateTime? businessDate = null)
        {
            var resource = "api/dashboard/summary";
            if (businessDate != null)
            {
                resource = $"{resource}?businessDate={Uri.EscapeDataString(businessDate.Value.ToString("yyyy-MM-dd"))}";
            }

            return RetrieveOneAsync<IgnitionDashboardSummaryDto>(resource, "Dashboard summary was empty.");
        }
    }
}
