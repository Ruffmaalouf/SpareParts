using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CarCatalogApiClient : FeatureApiClientBase, ICarCatalogApiClient
    {
        public CarCatalogApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.CatalogApiBaseUrl)
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

        public Task<List<UsedCarImageDto>> GetUsedCarImagesAsync(int usedCarId)
            => RetrieveAsync<UsedCarImageDto>($"api/usedcars/{usedCarId}/images");

        public async Task UploadUsedCarImageAsync(int usedCarId, string filePath)
        {
            var request = CreateRequest($"api/usedcars/{usedCarId}/images", Method.Post);
            var normalizedUpload = TryCreateExifNormalizedUpload(filePath);
            if (normalizedUpload == null)
            {
                request.AddFile("image", filePath, contentType: ApiClientBase.GetMimeType(filePath));
            }
            else
            {
                request.AddFile("image", normalizedUpload.Bytes, normalizedUpload.FileName, normalizedUpload.ContentType);
            }

            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"Upload used car image failed for {usedCarId}.");
        }

        public Task DeleteUsedCarImageAsync(int imageId)
            => DeleteResourceAsync($"api/usedcars/images/{imageId}");

        private static ExifNormalizedUpload? TryCreateExifNormalizedUpload(string filePath)
        {
            try
            {
                using var input = File.OpenRead(filePath);
                var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count == 0)
                {
                    return null;
                }

                var frame = decoder.Frames[0];
                var orientation = ReadExifOrientation(frame);
                if (orientation <= 1 || orientation > 8)
                {
                    return null;
                }

                var transformed = new TransformedBitmap(frame, CreateExifOrientationTransform(orientation));
                transformed.Freeze();

                var converted = new FormatConvertedBitmap(transformed, PixelFormats.Bgr24, null, 0);
                converted.Freeze();

                var encoder = new JpegBitmapEncoder
                {
                    QualityLevel = 92
                };
                encoder.Frames.Add(BitmapFrame.Create(converted));

                using var output = new MemoryStream();
                encoder.Save(output);

                return new ExifNormalizedUpload(
                    output.ToArray(),
                    $"{Path.GetFileNameWithoutExtension(filePath)}.jpg",
                    "image/jpeg");
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ushort ReadExifOrientation(BitmapFrame frame)
        {
            const string orientationQuery = "/app1/ifd/{ushort=274}";
            if (frame.Metadata is not BitmapMetadata metadata || !metadata.ContainsQuery(orientationQuery))
            {
                return 1;
            }

            return metadata.GetQuery(orientationQuery) switch
            {
                ushort value => value,
                short value => (ushort)value,
                uint value => (ushort)value,
                int value => (ushort)value,
                _ => 1
            };
        }

        private static Transform CreateExifOrientationTransform(ushort orientation)
            => orientation switch
            {
                2 => new ScaleTransform(-1, 1),
                3 => new RotateTransform(180),
                4 => new ScaleTransform(1, -1),
                5 => CreateTransformGroup(new ScaleTransform(-1, 1), new RotateTransform(90)),
                6 => new RotateTransform(90),
                7 => CreateTransformGroup(new ScaleTransform(-1, 1), new RotateTransform(270)),
                8 => new RotateTransform(270),
                _ => Transform.Identity
            };

        private static TransformGroup CreateTransformGroup(params Transform[] transforms)
        {
            var group = new TransformGroup();
            foreach (var transform in transforms)
            {
                group.Children.Add(transform);
            }

            return group;
        }

        private sealed record ExifNormalizedUpload(byte[] Bytes, string FileName, string ContentType);
    }
}
