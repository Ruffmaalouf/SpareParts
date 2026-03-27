using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            => new(new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false,
                Timeout = RequestTimeout,
                ConfigureMessageHandler = _ => new RetryHandler(
                    innerHandler: new HttpClientHandler(),
                    maxRetries: RetryCount,
                    baseDelay: RetryBaseDelay)
            });

        private sealed class RetryHandler(HttpMessageHandler innerHandler, int maxRetries, TimeSpan baseDelay)
            : DelegatingHandler(innerHandler)
        {
            private readonly int _maxRetries = maxRetries;
            private readonly TimeSpan _baseDelay = baseDelay;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var method = request.Method;

                for (var attempt = 0; ; attempt++)
                {
                    try
                    {
                        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                        if (attempt >= _maxRetries || !ShouldRetry(method, response.StatusCode))
                        {
                            return response;
                        }

                        response.Dispose();
                    }
                    catch (HttpRequestException) when (attempt < _maxRetries && IsIdempotent(method))
                    {
                        // retry
                    }
                    catch (TaskCanceledException) when (attempt < _maxRetries && IsIdempotent(method))
                    {
                        // retry
                    }

                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            private static bool ShouldRetry(HttpMethod method, HttpStatusCode statusCode)
                => IsIdempotent(method) &&
                   (statusCode == HttpStatusCode.TooManyRequests ||
                    statusCode == HttpStatusCode.RequestTimeout ||
                    (int)statusCode >= 500);

            private static bool IsIdempotent(HttpMethod method)
                => method == HttpMethod.Get ||
                   method == HttpMethod.Delete ||
                   method == HttpMethod.Head ||
                   method == HttpMethod.Options ||
                   method == HttpMethod.Put ||
                   method == HttpMethod.Trace;
        }
    }
}
