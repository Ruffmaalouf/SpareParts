using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>
/// Stripe Checkout integration using plain HTTPS calls to the Stripe REST API (no SDK dependency).
/// Disabled by default — <see cref="StripeProviderSettings.Enabled"/> must be true and a secret key
/// must be configured via environment/user-secrets before this provider can be used.
/// </summary>
public sealed class StripePaymentProvider : IPaymentProvider
{
    private static readonly Uri ApiBaseUri = new("https://api.stripe.com/v1/");

    private readonly HttpClient _httpClient;
    private readonly PaymentSettings _settings;

    public StripePaymentProvider(HttpClient httpClient, PaymentSettings settings)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= ApiBaseUri;
        _settings = settings;
    }

    public string ProviderCode => PaymentProviderCode.Stripe;

    public async Task<CheckoutResponseDto> CreateCheckoutAsync(PaymentCheckoutContext context, CancellationToken cancellationToken)
    {
        var stripeSettings = _settings.Providers.Stripe;
        if (!stripeSettings.Enabled || string.IsNullOrWhiteSpace(stripeSettings.SecretKey))
        {
            throw new ExternalServiceException("The Stripe payment provider is not configured.");
        }

        var unitAmount = (long)Math.Round(context.Amount * 100m, MidpointRounding.AwayFromZero);
        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = context.SuccessUrl,
            ["cancel_url"] = context.CancelUrl,
            ["client_reference_id"] = context.PaymentId.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = context.Currency.ToLowerInvariant(),
            ["line_items[0][price_data][unit_amount]"] = unitAmount.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"{context.PackageCode} plan ({context.BillingCycle})",
            ["metadata[paymentId]"] = context.PaymentId.ToString(CultureInfo.InvariantCulture),
            ["metadata[tenantId]"] = context.TenantId.ToString(CultureInfo.InvariantCulture)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "checkout/sessions")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stripeSettings.SecretKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceException($"Stripe checkout session creation failed: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var sessionId = root.GetProperty("id").GetString();
        var url = root.TryGetProperty("url", out var urlProperty) ? urlProperty.GetString() : null;

        return new CheckoutResponseDto
        {
            PaymentId = context.PaymentId,
            ProviderCode = ProviderCode,
            Status = "redirect",
            RedirectUrl = url,
            CheckoutSessionId = sessionId
        };
    }

    public Task<WebhookResult> HandleWebhookAsync(WebhookRequestContext context, CancellationToken cancellationToken)
    {
        var stripeSettings = _settings.Providers.Stripe;
        if (!stripeSettings.Enabled || string.IsNullOrWhiteSpace(stripeSettings.WebhookSecret))
        {
            return Task.FromResult(new WebhookResult { Success = false, Message = "Stripe webhooks are not configured." });
        }

        if (!context.Headers.TryGetValue("Stripe-Signature", out var signatureHeader) ||
            !VerifySignature(context.RawBody, signatureHeader, stripeSettings.WebhookSecret))
        {
            return Task.FromResult(new WebhookResult { Success = false, Message = "Invalid Stripe webhook signature." });
        }

        using var document = JsonDocument.Parse(context.RawBody);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? string.Empty;
        var eventType = root.GetProperty("type").GetString() ?? string.Empty;
        var dataObject = root.GetProperty("data").GetProperty("object");

        var providerPaymentId = dataObject.TryGetProperty("client_reference_id", out var referenceProperty)
            ? referenceProperty.GetString()
            : null;

        bool? succeeded = eventType switch
        {
            "checkout.session.completed" => true,
            "checkout.session.expired" => false,
            "payment_intent.payment_failed" => false,
            _ => null
        };

        return Task.FromResult(new WebhookResult
        {
            Success = true,
            Message = $"Processed Stripe event '{eventType}'.",
            ProviderEventId = eventId,
            ProviderPaymentId = providerPaymentId,
            EventType = eventType,
            PaymentSucceeded = succeeded
        });
    }

    private static bool VerifySignature(string payload, string signatureHeader, string webhookSecret)
    {
        string? timestamp = null;
        var signatures = new List<string>();

        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            if (pair[0] == "t") timestamp = pair[1];
            else if (pair[0] == "v1") signatures.Add(pair[1]);
        }

        if (timestamp is null || signatures.Count == 0)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var key = Encoding.UTF8.GetBytes(webhookSecret);
        var expectedBytes = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(expectedBytes).ToLowerInvariant();
        var expectedUtf8 = Encoding.UTF8.GetBytes(expected);

        return signatures.Any(sig => CryptographicOperations.FixedTimeEquals(expectedUtf8, Encoding.UTF8.GetBytes(sig)));
    }
}
