using System;
using System.IO;
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

        /// <summary>Base URL of SpareParts.Api, e.g. "http://localhost:5000/"</summary>
        public static string ApiBaseUrl { get; } = Load();

        private static string Load()
        {
            try
            {
                var path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "appsettings.json");

                if (!File.Exists(path)) return FallbackApiUrl;

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("ApiBaseUrl", out var v))
                    return v.GetString() ?? FallbackApiUrl;
            }
            catch { }
            return FallbackApiUrl;
        }
    }
}
