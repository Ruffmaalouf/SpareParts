using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.BusinessAssistant;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class BusinessAssistantApiClient : FeatureApiClientBase, IBusinessAssistantApiClient
    {
        public BusinessAssistantApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<BusinessAssistantResponseDto> AskAsync(BusinessAssistantQueryRequest request)
            => AddAsync<BusinessAssistantResponseDto>(
                "api/business-assistant/ask",
                request,
                "Business assistant did not return a response.");

        public Task<BusinessAssistantResponseDto> RunActionAsync(BusinessAssistantActionRequest request)
            => AddAsync<BusinessAssistantResponseDto>(
                "api/business-assistant/actions/run",
                request,
                "Business assistant action did not return a response.");
    }
}
