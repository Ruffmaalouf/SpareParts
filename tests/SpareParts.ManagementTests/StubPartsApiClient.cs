using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Scanning;

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

    public Task<DeadStockReportDto> GetDeadStockAsync(int minDormantDays = 90, int take = 25)
        => Task.FromResult(new DeadStockReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            MinDormantDays = minDormantDays
        });

    public Task<List<ScanLookupResultDto>> ResolveScanAsync(string code)
        => Task.FromResult(new List<ScanLookupResultDto>());

    public Task<List<PartStockDto>> GetPartStockAsync(int partId)
        => Task.FromResult(new List<PartStockDto>());

    public Task TransferPartAsync(int partId, TransferPartRequest request)
        => Task.CompletedTask;

    public Task UpdateUsedCarAsync(int partId, UpdatePartUsedCarRequest request)
        => Task.CompletedTask;

    public Task<GeneratePartNotesResponse> GeneratePartNotesAsync(GeneratePartNotesRequest request)
    {
        LastRequest = request;
        return Task.FromResult(Response);
    }
}
