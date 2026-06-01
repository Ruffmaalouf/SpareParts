using SpareParts.Domain.Growth;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IGrowthApiClient
    {
        Task<GrowthBriefingDto> GetBriefingAsync();
        Task<AuctionProfitSimulationDto> SimulateAuctionAsync(AuctionProfitSimulationRequest request);
        Task<VoiceQuoteDto> BuildVoiceQuoteAsync(VoiceQuoteRequest request);
    }
}
