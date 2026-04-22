using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;

namespace SpareParts.ManagementTests;

internal sealed class StubPartsApiClient : IPartsApiClient
{
    public GeneratePartNotesRequest? LastRequest { get; private set; }
    public GeneratePartNotesResponse Response { get; set; } = new()
    {
        SuggestedNotes = "Suggested AI notes.",
        SuggestedAveragePrice = 37.5m
    };

    public Task<List<PartDto>> GetPartsAsync() => Task.FromResult(new List<PartDto>());

    public Task<GeneratePartNotesResponse> GeneratePartNotesAsync(GeneratePartNotesRequest request)
    {
        LastRequest = request;
        return Task.FromResult(Response);
    }
}
