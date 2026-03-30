using System;
using System.Net.Http;
using System.Reflection;
using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.Desktop.Wpf
{
    public sealed class RestClientFactory : IRestClientFactory
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMilliseconds(250);
        private const int RetryCount = 3;

        public RestClient Create(string baseUrl)
        {
            var options = new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false,
                ConfigureMessageHandler = _ => new RetryHandler(
                    innerHandler: new HttpClientHandler(),
                    maxRetries: RetryCount,
                    baseDelay: RetryBaseDelay)
            };

            ApplyTimeout(options);

            return new RestClient(options);
        }

        private static void ApplyTimeout(RestClientOptions options)
        {
            var optionsType = options.GetType();

            var timeoutProperty = optionsType.GetProperty("Timeout", BindingFlags.Public | BindingFlags.Instance);
            if (timeoutProperty is not null && timeoutProperty.CanWrite && timeoutProperty.PropertyType == typeof(TimeSpan?))
            {
                timeoutProperty.SetValue(options, RequestTimeout);
                return;
            }

            var maxTimeoutProperty = optionsType.GetProperty("MaxTimeout", BindingFlags.Public | BindingFlags.Instance);
            if (maxTimeoutProperty is not null && maxTimeoutProperty.CanWrite && maxTimeoutProperty.PropertyType == typeof(int))
            {
                maxTimeoutProperty.SetValue(options, (int)RequestTimeout.TotalMilliseconds);
            }
        }
    }
}
