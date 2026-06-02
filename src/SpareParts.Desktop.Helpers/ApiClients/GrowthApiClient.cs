using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Growth;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class GrowthApiClient : FeatureApiClientBase, IGrowthApiClient
    {
        public GrowthApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<GrowthBriefingDto> GetBriefingAsync()
            => RetrieveOneAsync<GrowthBriefingDto>("api/growth/briefing", "Money Finder briefing was empty.");

        public Task<AuctionProfitSimulationDto> SimulateAuctionAsync(AuctionProfitSimulationRequest request)
            => AddAsync<AuctionProfitSimulationDto>("api/growth/auction-simulator", request, "Auction simulation was empty.");

        public Task<VoiceQuoteDto> BuildVoiceQuoteAsync(VoiceQuoteRequest request)
            => AddAsync<VoiceQuoteDto>("api/growth/voice-quote", request, "Voice quote was empty.");
    }
}
