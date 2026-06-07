using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RestSharp;

namespace SpareParts.Desktop.Wpf
{
    public sealed class PartsApiClient : FeatureApiClientBase, IPartsApiClient
    {
        public PartsApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.InventoryApiBaseUrl)
        {
        }

        public Task<List<PartDto>> GetPartsAsync() => RetrieveAllPagesAsync<PartDto>("api/parts");

        public Task<DeadStockReportDto> GetDeadStockAsync(int minDormantDays = 90, int take = 25)
            => RetrieveOneAsync<DeadStockReportDto>($"api/parts/dead-stock?minDormantDays={minDormantDays}&take={take}", "Dead stock report was empty.");

        public Task<PartListingPackageDto> GetListingPackageAsync(int partId)
            => RetrieveOneAsync<PartListingPackageDto>($"api/parts/{partId}/listing-package", "Listing package was empty.");

        public Task<List<ScanLookupResultDto>> ResolveScanAsync(string code)
            => RetrieveAsync<ScanLookupResultDto>($"api/scans/resolve?code={Uri.EscapeDataString(code ?? string.Empty)}");

        public async Task<VisualPartSearchResponseDto> SearchPartsByImageAsync(string imagePath, string? hint = null, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("Choose an image file before searching.", imagePath);
            }

            var request = CreateRequest("api/scans/visual-search", Method.Post);
            request.AlwaysMultipartFormData = true;
            request.AddFile("image", imagePath);
            request.AddParameter("hint", hint ?? string.Empty);
            request.AddParameter("limit", limit);

            var response = await Client.ExecuteAsync<VisualPartSearchResponseDto>(request);
            ApiClientBase.EnsureSuccess(response, "POST api/scans/visual-search failed.");
            return response.Data ?? new VisualPartSearchResponseDto { Message = "Picture search returned no response." };
        }

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
