using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using SpareParts.Domain.Common;
using SpareParts.Domain.Scanning;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class VisualPartSearchService
{
    private const int MaxImageBytes = 8 * 1024 * 1024;
    private const int MaxTokens = 12;

    private const string VisionInstructions = """
        You identify automotive spare parts from a shop-counter photo.
        Focus on the visible part family, shape, visible labels, OEM markings, and likely synonyms.
        Do not claim exact fitment unless the image or user hint makes it visible.
        Return compact search terms that can match an ERP parts catalog.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "ar", "auto", "automotive", "by", "for", "from", "heic",
        "heif", "image", "img", "in", "is", "it", "jpeg", "jpg", "of", "on", "or", "part", "parts", "photo", "picture", "png",
        "search", "the", "this", "to", "webp", "with"
    };

    private readonly ISqlConnectionFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ITenantContext _tenantContext;

    public VisualPartSearchService(
        ISqlConnectionFactory factory,
        HttpClient httpClient,
        OpenAiOptions options,
        ITenantContext tenantContext)
    {
        _factory = factory;
        _httpClient = httpClient;
        _options = options;
        _tenantContext = tenantContext;
    }

    public async Task<VisualPartSearchResponseDto> SearchAsync(
        Stream imageStream,
        string? fileName,
        string? contentType,
        string? hint,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var imageBytes = await ReadImageBytesAsync(imageStream, cancellationToken);
        var normalizedContentType = NormalizeContentType(contentType);
        var recognition = await TryRecognizeAsync(imageBytes, normalizedContentType, hint, cancellationToken);
        var tokens = ExtractSearchTokens(hint, fileName, recognition.SearchText, recognition.PartFamily, string.Join(' ', recognition.Tags));
        var candidates = QueryPartCandidates();
        var matches = RankCandidates(candidates, tokens, recognition.Tags, Math.Clamp(limit, 1, 25));
        var searchText = string.Join(' ', tokens);

        return new VisualPartSearchResponseDto
        {
            Source = recognition.Source,
            SearchText = searchText,
            VisualTags = recognition.Tags.ToList(),
            Matches = matches,
            Message = BuildMessage(recognition, tokens.Count, matches.Count)
        };
    }

    public static IReadOnlyList<string> ExtractSearchTokens(params string?[] values)
    {
        var tokens = new List<string>();

        foreach (var value in values)
        {
            foreach (var token in Tokenize(value))
            {
                if (tokens.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                tokens.Add(token);
                if (tokens.Count >= MaxTokens)
                {
                    return tokens;
                }
            }
        }

        return tokens;
    }

    private static async Task<byte[]> ReadImageBytesAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        if (bytes.Length == 0)
        {
            throw new ValidationException("Image is required for visual part search.");
        }

        if (bytes.Length > MaxImageBytes)
        {
            throw new ValidationException("Image must be 8 MB or smaller.");
        }

        return bytes;
    }

    private static string NormalizeContentType(string? contentType)
    {
        var normalized = string.IsNullOrWhiteSpace(contentType)
            ? "image/jpeg"
            : contentType.Trim().ToLowerInvariant();

        if (!normalized.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Upload a JPG, PNG, WebP, or HEIC image.");
        }

        return normalized;
    }

    private async Task<VisualRecognitionResult> TryRecognizeAsync(
        byte[] imageBytes,
        string contentType,
        string? hint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return VisualRecognitionResult.Metadata("AI vision is not configured; matching by your hint and image name.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "responses")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(BuildVisionRequestBody(imageBytes, contentType, hint), JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            HttpResponseMessage response;
            string rawContent;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
                rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException)
            {
                return VisualRecognitionResult.Metadata("AI vision was unreachable; matching by your hint and image name.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return VisualRecognitionResult.Metadata("AI vision request timed out; matching by your hint and image name.");
            }

            using var __ = response;

            if (!response.IsSuccessStatusCode)
            {
                return VisualRecognitionResult.Metadata($"AI vision was unavailable ({(int)response.StatusCode}); matching by your hint and image name.");
            }

            var structuredPayload = ReadStructuredPayload(rawContent);
            if (string.IsNullOrWhiteSpace(structuredPayload))
            {
                return VisualRecognitionResult.Metadata("AI vision returned no labels; matching by your hint and image name.");
            }

            var result = JsonSerializer.Deserialize<VisualRecognitionPayload>(structuredPayload, JsonOptions);
            if (result == null)
            {
                return VisualRecognitionResult.Metadata("AI vision labels were unreadable; matching by your hint and image name.");
            }

            var tags = ExtractSearchTokens(result.PartFamily, result.SearchText, string.Join(' ', result.Tags ?? []))
                .Take(8)
                .ToArray();

            return new VisualRecognitionResult(
                "ai-vision",
                result.SearchText?.Trim() ?? string.Empty,
                result.PartFamily?.Trim() ?? string.Empty,
                tags,
                "Matched from AI image labels.");
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            return VisualRecognitionResult.Metadata($"AI vision could not read the image; matching by your hint and image name. {ex.Message}");
        }
    }

    private object BuildVisionRequestBody(byte[] imageBytes, string contentType, string? hint)
    {
        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        var userPrompt = string.IsNullOrWhiteSpace(hint)
            ? "Identify the visible automotive part and return useful catalog search tags."
            : $"Identify the visible automotive part. User hint: {hint.Trim()}";

        return new
        {
            model = _options.Model,
            input = new object[]
            {
                new
                {
                    role = "developer",
                    content = VisionInstructions
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = userPrompt },
                        new { type = "input_image", image_url = dataUrl }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "visual_part_search",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            partFamily = new
                            {
                                type = "string",
                                description = "The most likely visible automotive part family."
                            },
                            searchText = new
                            {
                                type = "string",
                                description = "Short catalog search text, including visible codes or markings when present."
                            },
                            tags = new
                            {
                                type = "array",
                                items = new { type = "string" },
                                description = "Synonyms and visual attributes useful for matching catalog records."
                            },
                            confidence = new
                            {
                                type = "number",
                                description = "0 to 1 confidence in the visual identification."
                            }
                        },
                        required = new[] { "partFamily", "searchText", "tags", "confidence" }
                    }
                }
            }
        };
    }

    private static string? ReadStructuredPayload(string rawContent)
    {
        var envelope = JsonSerializer.Deserialize<OpenAiResponseEnvelope>(rawContent, JsonOptions);
        var refusal = envelope?.Output?
            .Where(item => string.Equals(item.Type, "message", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.Content ?? [])
            .FirstOrDefault(item => string.Equals(item.Type, "refusal", StringComparison.OrdinalIgnoreCase))
            ?.Refusal;

        if (!string.IsNullOrWhiteSpace(refusal))
        {
            return null;
        }

        return envelope?.Output?
            .Where(item => string.Equals(item.Type, "message", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.Content ?? [])
            .FirstOrDefault(item => string.Equals(item.Type, "output_text", StringComparison.OrdinalIgnoreCase))
            ?.Text;
    }

    private IReadOnlyList<VisualPartCandidate> QueryPartCandidates()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var rows = session.Connection.Query<VisualPartCandidate>(
            """
            WITH StockByPart AS
            (
                SELECT
                    PartId,
                    AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
                FROM dbo.Stock
                WHERE (@TenantId = 0 OR TenantId = @TenantId)
                GROUP BY PartId
            )
            SELECT TOP (1500)
                   p.Id AS PartId,
                   p.InternalCode,
                   p.Barcode,
                   p.Name AS PartName,
                   p.OEMNumber AS OemNumber,
                   b.Name AS BrandName,
                   c.Name AS CategoryName,
                   p.Notes,
                   p.SalePrice,
                   ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                   ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity
            FROM dbo.Parts p
            LEFT JOIN dbo.Brands b ON b.Id = p.BrandId AND (@TenantId = 0 OR b.TenantId = @TenantId)
            LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId AND (@TenantId = 0 OR c.TenantId = @TenantId)
            LEFT JOIN StockByPart stock ON stock.PartId = p.Id
            WHERE p.IsActive = 1 AND (@TenantId = 0 OR p.TenantId = @TenantId)
            ORDER BY
                CASE WHEN ISNULL(stock.AvailableQuantity, 0) > 0 THEN 0 ELSE 1 END,
                p.Name;
            """,
            new { session.TenantId },
            transaction: session.Transaction).ToList();

        session.Commit();
        return rows;
    }

    private static List<VisualPartMatchDto> RankCandidates(
        IReadOnlyList<VisualPartCandidate> candidates,
        IReadOnlyList<string> tokens,
        IReadOnlyList<string> visualTags,
        int limit)
    {
        return candidates
            .Select(candidate => ScoreCandidate(candidate, tokens, visualTags))
            .Where(match => tokens.Count == 0 || match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.AvailableQuantity)
            .ThenBy(match => match.PartName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static VisualPartMatchDto ScoreCandidate(
        VisualPartCandidate candidate,
        IReadOnlyList<string> tokens,
        IReadOnlyList<string> visualTags)
    {
        var searchable = string.Join(' ', new[]
        {
            candidate.InternalCode,
            candidate.Barcode,
            candidate.PartName,
            candidate.OemNumber,
            candidate.BrandName,
            candidate.CategoryName,
            candidate.Notes
        }).ToLowerInvariant();

        var reasons = new List<string>();
        decimal score = 0;

        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            var normalized = token.ToLowerInvariant();
            if (FieldEquals(candidate.InternalCode, normalized) ||
                FieldEquals(candidate.Barcode, normalized) ||
                FieldEquals(candidate.OemNumber, normalized))
            {
                score += 35;
                reasons.Add($"exact {token}");
            }
            else if (FieldContains(candidate.InternalCode, normalized) ||
                     FieldContains(candidate.OemNumber, normalized))
            {
                score += 18;
                reasons.Add($"code {token}");
            }
            else if (searchable.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            {
                score += visualTags.Contains(token, StringComparer.OrdinalIgnoreCase) ? 12 : 8;
                reasons.Add(token);
            }
        }

        if (candidate.AvailableQuantity > 0)
        {
            score += 2;
        }

        if (score == 0 && tokens.Count == 0)
        {
            score = candidate.AvailableQuantity > 0 ? 2 : 1;
            reasons.Add("low-confidence shortlist");
        }

        var anchorSeed = Math.Abs($"{candidate.PartId}:{candidate.InternalCode}".GetHashCode());
        return new VisualPartMatchDto
        {
            PartId = candidate.PartId,
            InternalCode = candidate.InternalCode,
            Barcode = candidate.Barcode,
            PartName = candidate.PartName,
            OemNumber = candidate.OemNumber,
            BrandName = candidate.BrandName,
            CategoryName = candidate.CategoryName,
            SalePrice = candidate.SalePrice,
            Currency = candidate.Currency,
            AvailableQuantity = candidate.AvailableQuantity,
            Score = score,
            MatchReason = reasons.Count == 0 ? "Visual shortlist" : string.Join(", ", reasons.Distinct(StringComparer.OrdinalIgnoreCase).Take(4)),
            AnchorX = 0.18 + (anchorSeed % 58) / 100.0,
            AnchorY = 0.16 + ((anchorSeed / 7) % 56) / 100.0
        };
    }

    private static IEnumerable<string> Tokenize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var builder = new StringBuilder();
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        foreach (var token in builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Length < 2 || StopWords.Contains(token))
            {
                continue;
            }

            yield return token;
        }
    }

    private static bool FieldEquals(string? value, string token)
        => !string.IsNullOrWhiteSpace(value) &&
           string.Equals(value.Trim(), token, StringComparison.OrdinalIgnoreCase);

    private static bool FieldContains(string? value, string token)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static string BuildMessage(VisualRecognitionResult recognition, int tokenCount, int matchCount)
    {
        if (matchCount == 0)
        {
            return tokenCount == 0
                ? $"{recognition.Message} Add a hint such as BMW headlight or brake sensor and try again."
                : "No catalog parts matched the picture search terms.";
        }

        return recognition.Message;
    }

    private sealed record VisualRecognitionResult(
        string Source,
        string SearchText,
        string PartFamily,
        IReadOnlyList<string> Tags,
        string Message)
    {
        public static VisualRecognitionResult Metadata(string message)
            => new("metadata", string.Empty, string.Empty, Array.Empty<string>(), message);
    }

    private sealed class VisualRecognitionPayload
    {
        public string? PartFamily { get; set; }
        public string? SearchText { get; set; }
        public List<string>? Tags { get; set; }
        public decimal Confidence { get; set; }
    }

    private sealed class VisualPartCandidate
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
        public string? Notes { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int AvailableQuantity { get; set; }
    }
}
