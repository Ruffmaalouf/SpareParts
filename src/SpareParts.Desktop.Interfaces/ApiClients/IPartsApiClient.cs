using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Scanning;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IPartsApiClient
    {
        Task<List<PartDto>> GetPartsAsync();
        Task<DeadStockReportDto> GetDeadStockAsync(int minDormantDays = 90, int take = 25);
        Task<PartListingPackageDto> GetListingPackageAsync(int partId);
        Task<List<ScanLookupResultDto>> ResolveScanAsync(string code);
        Task<VisualPartSearchResponseDto> SearchPartsByImageAsync(string imagePath, string? hint = null, int limit = 10);
        Task<List<PartStockDto>> GetPartStockAsync(int partId);
        Task TransferPartAsync(int partId, TransferPartRequest request);
        Task UpdateUsedCarAsync(int partId, UpdatePartUsedCarRequest request);
        Task<GeneratePartNotesResponse> GeneratePartNotesAsync(GeneratePartNotesRequest request);
    }
}
