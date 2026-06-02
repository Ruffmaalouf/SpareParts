using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Reads appsettings.json next to the exe.
    /// The WPF frontend only needs the API base URL — no DB connection string.
    /// </summary>
    public static class AppSettings
    {
        private const string FallbackApiUrl = "http://localhost:5000/";
        private const string FallbackPublicWebUrl = "http://localhost:5078/";
        private static readonly IReadOnlyDictionary<string, string> _serviceEndpoints = LoadServiceEndpoints();

        /// <summary>Legacy base URL support (single-host mode).</summary>
        public static string ApiBaseUrl { get; } = _serviceEndpoints["monolith"];
        public static string SalesApiBaseUrl => _serviceEndpoints["sales"];
        public static string PurchasesApiBaseUrl => _serviceEndpoints["purchases"];
        public static string InventoryApiBaseUrl => _serviceEndpoints["inventory"];
        public static string IdentityApiBaseUrl => _serviceEndpoints["identity"];
        public static string CatalogApiBaseUrl => _serviceEndpoints["catalog"];
        public static string PublicWebBaseUrl => _serviceEndpoints["web"];

        public static IReadOnlyDictionary<string, string> ServiceEndpoints => _serviceEndpoints;

        private static IReadOnlyDictionary<string, string> LoadServiceEndpoints()
        {
            var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["monolith"] = FallbackApiUrl,
                ["sales"] = FallbackApiUrl,
                ["purchases"] = FallbackApiUrl,
                ["inventory"] = FallbackApiUrl,
                ["identity"] = FallbackApiUrl,
                ["catalog"] = FallbackApiUrl,
                ["web"] = FallbackPublicWebUrl
            };

            try
            {
                var path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "appsettings.json");

                if (!File.Exists(path)) return defaults;

                using var doc = JsonDocument.Parse(File.ReadAllText(path));

                var root = doc.RootElement;
                var monolithUrl = ReadUrl(root, "ApiBaseUrl", FallbackApiUrl);

                defaults["monolith"] = monolithUrl;
                defaults["sales"] = ReadUrl(root, "SalesApiBaseUrl", monolithUrl);
                defaults["purchases"] = ReadUrl(root, "PurchasesApiBaseUrl", monolithUrl);
                defaults["inventory"] = ReadUrl(root, "InventoryApiBaseUrl", monolithUrl);
                defaults["identity"] = ReadUrl(root, "IdentityApiBaseUrl", monolithUrl);
                defaults["catalog"] = ReadUrl(root, "CatalogApiBaseUrl", monolithUrl);
                defaults["web"] = ReadUrl(root, "PublicWebBaseUrl", FallbackPublicWebUrl);
            }
            catch
            {
                return defaults;
            }

            return defaults.ToDictionary(kvp => kvp.Key, kvp => NormalizeUrl(kvp.Value), StringComparer.OrdinalIgnoreCase);
        }

        private static string ReadUrl(JsonElement root, string propertyName, string fallback)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                return fallback;
            }

            var url = value.GetString();
            return string.IsNullOrWhiteSpace(url) ? fallback : url;
        }

        private static string NormalizeUrl(string value)
        {
            var url = string.IsNullOrWhiteSpace(value) ? FallbackApiUrl : value.Trim();
            return url.EndsWith('/') ? url : $"{url}/";
        }
    }
}
