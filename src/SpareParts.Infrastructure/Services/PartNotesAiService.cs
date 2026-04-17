using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Services;

public sealed class PartNotesAiService
{
    private const string DraftInstructions = """
        You draft concise catalog notes for an automotive spare-parts ERP.
        Use only the facts provided in the request.
        If data is missing, stay general rather than inventing specifics.
        Do not invent exact vehicle fitment, material composition, warranty terms, certifications, or country of origin.
        If existing notes are provided, improve clarity while preserving the factual content.
        Estimate a reasonable average market selling price in the same currency when enough context exists.
        If you cannot estimate responsibly, return null for the average price.
        Return 2 to 4 short sentences suitable for a free-text Notes field.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public PartNotesAiService(HttpClient httpClient, OpenAiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<GeneratePartNotesResponse> GenerateNotesAsync(
        GeneratePartNotesRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        EnsureRequestIsValid(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(BuildRequestBody(request), JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateExternalServiceException(response.StatusCode, rawContent);
        }

        var envelope = JsonSerializer.Deserialize<OpenAiResponseEnvelope>(rawContent, JsonOptions)
            ?? throw new ExternalServiceException("The AI provider returned an unreadable response.");

        var refusal = envelope.Output?
            .Where(item => string.Equals(item.Type, "message", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.Content ?? [])
            .FirstOrDefault(item => string.Equals(item.Type, "refusal", StringComparison.OrdinalIgnoreCase))
            ?.Refusal;

        if (!string.IsNullOrWhiteSpace(refusal))
        {
            throw new ValidationException(refusal);
        }

        var structuredPayload = envelope.Output?
            .Where(item => string.Equals(item.Type, "message", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.Content ?? [])
            .FirstOrDefault(item => string.Equals(item.Type, "output_text", StringComparison.OrdinalIgnoreCase))
            ?.Text;

        if (string.IsNullOrWhiteSpace(structuredPayload))
        {
            throw new ExternalServiceException("The AI provider returned an empty suggestion.");
        }

        var suggestion = JsonSerializer.Deserialize<GeneratePartNotesResponse>(structuredPayload, JsonOptions)
            ?? throw new ExternalServiceException("The AI provider returned invalid structured output.");

        suggestion.SuggestedNotes = suggestion.SuggestedNotes.Trim();
        if (string.IsNullOrWhiteSpace(suggestion.SuggestedNotes))
        {
            throw new ExternalServiceException("The AI provider did not return usable notes.");
        }

        return suggestion;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ValidationException("AI part notes are not configured on the server. Set OpenAI:ApiKey or OPENAI_API_KEY.");
        }
    }

    private static void EnsureRequestIsValid(GeneratePartNotesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InternalCode))
        {
            throw new ValidationException("Part code is required to generate AI notes.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Part name is required to generate AI notes.");
        }
    }

    private object BuildRequestBody(GeneratePartNotesRequest request)
    {
        return new
        {
            model = _options.Model,
            input = new object[]
            {
                new
                {
                    role = "developer",
                    content = DraftInstructions
                },
                new
                {
                    role = "user",
                    content = BuildPrompt(request)
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "part_notes_suggestion",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            suggestedNotes = new
                            {
                                type = "string",
                                description = "A concise, factual note draft for the part catalog."
                            },
                            suggestedAveragePrice = new
                            {
                                anyOf = new object[]
                                {
                                    new
                                    {
                                        type = "number"
                                    },
                                    new
                                    {
                                        type = "null"
                                    }
                                },
                                description = "Estimated average selling price in the same currency, or null when unavailable."
                            }
                        },
                        required = new[] { "suggestedNotes", "suggestedAveragePrice" }
                    }
                }
            }
        };
    }

    private static string BuildPrompt(GeneratePartNotesRequest request)
    {
        var lines = new[]
        {
            $"Internal code: {request.InternalCode.Trim()}",
            $"Part name: {request.Name.Trim()}",
            $"OEM number: {FormatValue(request.OEMNumber)}",
            $"Category: {FormatValue(request.CategoryName)}",
            $"Brand: {FormatValue(request.BrandName)}",
            $"Cost price: {request.CostPrice:0.##} {request.Currency}",
            $"Sale price: {request.SalePrice:0.##} {request.Currency}",
            $"Minimum stock: {request.MinStock}",
            $"Existing notes: {FormatValue(request.ExistingNotes)}"
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim();

    private static ExternalServiceException CreateExternalServiceException(HttpStatusCode statusCode, string rawContent)
    {
        var message = TryGetOpenAiErrorMessage(rawContent)
            ?? $"The AI provider returned {(int)statusCode} ({statusCode}).";

        return new ExternalServiceException(message);
    }

    private static string? TryGetOpenAiErrorMessage(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return null;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<OpenAiErrorEnvelope>(rawContent, JsonOptions);
            if (!string.IsNullOrWhiteSpace(envelope?.Error?.Message))
            {
                return envelope.Error.Message.Trim();
            }
        }
        catch (JsonException)
        {
            // Fall back to a generic message below.
        }

        return null;
    }

    private sealed class OpenAiResponseEnvelope
    {
        public List<OpenAiOutputItem>? Output { get; set; }
    }

    private sealed class OpenAiOutputItem
    {
        public string? Type { get; set; }
        public List<OpenAiContentItem>? Content { get; set; }
    }

    private sealed class OpenAiContentItem
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
        public string? Refusal { get; set; }
    }

    private sealed class OpenAiErrorEnvelope
    {
        public OpenAiErrorPayload? Error { get; set; }
    }

    private sealed class OpenAiErrorPayload
    {
        public string? Message { get; set; }
    }
}
