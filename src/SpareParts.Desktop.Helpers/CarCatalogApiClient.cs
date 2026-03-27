using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CarCatalogApiClient : FeatureApiClientBase, ICarCatalogApiClient
    {
        public CarCatalogApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<CarBrandDto>> GetCarBrandsAsync() => RetrieveAsync<CarBrandDto>("api/carbrands");

        public async Task<BitmapImage?> GetCarBrandLogoAsync(int brandId)
        {
            var request = CreateRequest($"api/carbrands/{brandId}/logo", Method.Get);
            var response = await Client.ExecuteAsync(request);
            if (!response.IsSuccessful || response.RawBytes is null)
            {
                return null;
            }

            return ApiClientBase.BytesToBitmap(response.RawBytes);
        }

        public async Task UploadCarBrandLogoAsync(int brandId, string filePath)
        {
            var request = CreateRequest($"api/carbrands/{brandId}/logo", Method.Post);
            request.AddFile("image", filePath, contentType: ApiClientBase.GetMimeType(filePath));

            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"Upload brand logo failed for {brandId}.");
        }

        public Task<List<CarModelDto>> GetCarModelsAsync(int brandId)
            => RetrieveAsync<CarModelDto>($"api/carmodels?brandId={brandId}");

        public async Task<BitmapImage?> GetCarModelImageAsync(int modelId)
        {
            var request = CreateRequest($"api/carmodels/{modelId}/image", Method.Get);
            var response = await Client.ExecuteAsync(request);
            if (!response.IsSuccessful || response.RawBytes is null)
            {
                return null;
            }

            return ApiClientBase.BytesToBitmap(response.RawBytes);
        }

        public async Task UploadCarModelImageAsync(int modelId, string filePath)
        {
            var request = CreateRequest($"api/carmodels/{modelId}/image", Method.Post);
            request.AddFile("image", filePath, contentType: ApiClientBase.GetMimeType(filePath));

            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"Upload model image failed for {modelId}.");
        }
    }
}
