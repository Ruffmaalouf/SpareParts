using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Scanning;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class PartsApiClient : FeatureApiClientBase, IPartsApiClient
    {
        public PartsApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.InventoryApiBaseUrl)
        {
        }

        public Task<List<PartDto>> GetPartsAsync() => RetrieveAsync<PartDto>("api/parts");

        public Task<DeadStockReportDto> GetDeadStockAsync(int minDormantDays = 90, int take = 25)
            => RetrieveOneAsync<DeadStockReportDto>($"api/parts/dead-stock?minDormantDays={minDormantDays}&take={take}", "Dead stock report was empty.");

        public Task<List<ScanLookupResultDto>> ResolveScanAsync(string code)
            => RetrieveAsync<ScanLookupResultDto>($"api/scans/resolve?code={Uri.EscapeDataString(code ?? string.Empty)}");

        public Task<List<PartStockDto>> GetPartStockAsync(int partId)
            => RetrieveAsync<PartStockDto>($"api/parts/{partId}/stock");

        public Task TransferPartAsync(int partId, TransferPartRequest request)
            => AddAsync($"api/parts/{partId}/transfer", request);

        public Task UpdateUsedCarAsync(int partId, UpdatePartUsedCarRequest request)
            => EditAsync($"api/parts/{partId}/usedcar", request);

        public Task<GeneratePartNotesResponse> GeneratePartNotesAsync(GeneratePartNotesRequest request)
            => AddAsync<GeneratePartNotesResponse>("api/parts/ai/notes", request, "AI did not return part notes.");
    }
}
