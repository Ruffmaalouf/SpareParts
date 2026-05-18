using System.Net.Http.Json;

namespace SpareParts.Infrastructure.Services
{
    public sealed class WebhookCommunicationDeliveryClient : ICommunicationDeliveryClient
    {
        private readonly HttpClient _client;
        private readonly CommunicationOptions _options;

        public WebhookCommunicationDeliveryClient(HttpClient client, CommunicationOptions options)
        {
            _client = client;
            _options = options;
        }

        public async Task<CommunicationDeliveryResult> SendAsync(
            CommunicationPreparedMessage message,
            CancellationToken cancellationToken)
        {
            if (!_options.HasWebhook)
            {
                return new CommunicationDeliveryResult
                {
                    Status = "Prepared",
                    Provider = "Webhook",
                    ProviderStatus = "NotConfigured",
                    ErrorMessage = "Communication webhook is not configured; message was prepared and logged only."
                };
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.WebhookUrl);
            if (!string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                request.Headers.Add("X-SpareParts-Communication-Secret", _options.WebhookSecret);
            }

            request.Content = JsonContent.Create(message);

            try
            {
                using var response = await _client.SendAsync(request, cancellationToken);
                var providerStatus = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
                var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new CommunicationDeliveryResult
                    {
                        Status = "Failed",
                        Provider = "Webhook",
                        ProviderStatus = providerStatus,
                        ErrorMessage = string.IsNullOrWhiteSpace(rawResponse) ? providerStatus : rawResponse
                    };
                }

                return new CommunicationDeliveryResult
                {
                    Status = "Sent",
                    Provider = "Webhook",
                    ProviderStatus = providerStatus,
                    ProviderMessageId = TryReadProviderMessageId(rawResponse),
                    SentAt = DateTime.UtcNow
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new CommunicationDeliveryResult
                {
                    Status = "Failed",
                    Provider = "Webhook",
                    ProviderStatus = "Exception",
                    ErrorMessage = ex.Message
                };
            }
        }

        private static string? TryReadProviderMessageId(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return null;
            }

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rawResponse);
                if (doc.RootElement.TryGetProperty("messageId", out var messageId)
                    || doc.RootElement.TryGetProperty("id", out messageId))
                {
                    return messageId.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }

            return null;
        }
    }
}
