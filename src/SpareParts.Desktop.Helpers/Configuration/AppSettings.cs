using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Reads appsettings.json next to the exe.
    /// The WPF frontend only needs the API base URL — no DB connection string.
    ///
    /// NOTE (security): the "http://localhost" defaults below are acceptable ONLY for local
    /// development (loopback traffic never leaves the machine). Any production/staging
    /// deployment profile MUST configure https:// endpoints — see EnsureSecureEndpoint(),
    /// which rejects non-loopback http:// URLs at load time so a misconfigured/shipped
    /// appsettings.json can't silently send credentials or data over plaintext HTTP.
    /// </summary>
    public static class AppSettings
    {
        private const string FallbackApiUrl = "http://localhost:5000/";
        private const string FallbackPublicWebUrl = "http://localhost:5078/";

        /// <summary>Hosts allowed to use http:// (unencrypted) because traffic never leaves the machine/emulator.</summary>
        private static readonly string[] LoopbackHosts = { "localhost", "127.0.0.1", "::1", "10.0.2.2" };

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

            return defaults.ToDictionary(kvp => kvp.Key, kvp => EnsureSecureEndpoint(kvp.Key, NormalizeUrl(kvp.Value)), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Guards against shipping/configuring a non-loopback endpoint over plain http://.
        /// Loopback (localhost/127.0.0.1/::1/10.0.2.2 — the Android emulator alias for the host loopback)
        /// is allowed over http for local dev. Any other host must use https://; if it doesn't, we
        /// loudly warn (Trace + Debug) and fail fast in a Release build so the misconfiguration is
        /// caught before it can leak traffic (auth tokens, tenant data) over an unencrypted channel.
        /// </summary>
        private static string EnsureSecureEndpoint(string endpointName, string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url;
            }

            var isHttp = string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase);
            var isLoopbackHost = LoopbackHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);

            if (isHttp && !isLoopbackHost)
            {
                var message = $"[SpareParts.Desktop] Insecure endpoint configured for '{endpointName}': '{url}'. " +
                               "Non-loopback endpoints must use https://. Update appsettings.json for this environment.";

                Trace.TraceWarning(message);
                Debug.WriteLine(message);

#if !DEBUG
                throw new InvalidOperationException(message);
#endif
            }

            return url;
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
