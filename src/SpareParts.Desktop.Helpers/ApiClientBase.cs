using RestSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    internal static class ApiClientBase
    {
        internal static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var raw = await response.Content.ReadAsStringAsync();
            ThrowApiClientException(raw, fallbackMessage);
        }

        internal static void EnsureSuccess(RestResponse response, string fallbackMessage)
        {
            if (response.IsSuccessful)
            {
                return;
            }

            ThrowApiClientException(response.Content, fallbackMessage);
        }

        private static void ThrowApiClientException(string? rawContent, string fallbackMessage)
        {
            var raw = rawContent ?? string.Empty;
            try
            {
                var envelope = JsonSerializer.Deserialize<ApiErrorEnvelope>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope != null && !string.IsNullOrWhiteSpace(envelope.Code))
                {
                    throw new ApiClientException(envelope.Code, envelope.Message, envelope.TraceId);
                }
            }
            catch (JsonException)
            {
                // ignore and fallback below
            }

            throw new ApiClientException("http_error", string.IsNullOrWhiteSpace(raw) ? fallbackMessage : raw.Trim('"', ' ', '\n'));
        }

        internal static BitmapImage BytesToBitmap(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        internal static string GetMimeType(string path) =>
            Path.GetExtension(path).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "image/png"
            };
    }
}
