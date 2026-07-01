using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpareParts.ImageEnrichment;

internal sealed record SearchQuery(string Text, QueryTier Tier);

internal enum QueryTier
{
    Generic = 1,
    VehiclePart = 2,
    BrandPart = 3,
    ExactIdentifier = 4
}

internal sealed record ImageCandidate(
    string ImageUrl,
    string? ThumbUrl,
    string? SourcePageUrl,
    string SourceDomain,
    string Title,
    string Provider,
    string? License,
    bool LicenseKnown,
    bool UnknownLicenseCatalogSource);

internal static class QueryBuilder
{
    public static IReadOnlyList<SearchQuery> Build(PartRecord part)
    {
        var queries = new List<SearchQuery>();
        var name = Clean(part.Name);
        var make = Clean(part.CarMake);
        var model = Clean(part.CarModel);
        var year = part.CarYear?.ToString();
        var brand = Clean(part.Brand);
        var category = Clean(part.Category);
        var engine = Clean(part.EngineType);

        foreach (var id in part.ExternalIdentifiers)
        {
            queries.Add(new SearchQuery($"{id} {name}", QueryTier.ExactIdentifier));
            if (!string.IsNullOrWhiteSpace(make))
            {
                queries.Add(new SearchQuery($"{id} {make} {model} {name}", QueryTier.ExactIdentifier));
            }
        }

        if (!string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(year))
        {
            queries.Add(new SearchQuery($"{make} {model} {year} {name}", QueryTier.VehiclePart));
            if (!string.IsNullOrWhiteSpace(engine))
            {
                queries.Add(new SearchQuery($"{make} {model} {year} {engine} {name} used genuine part", QueryTier.VehiclePart));
            }
        }

        if (!string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(model))
        {
            queries.Add(new SearchQuery($"{make} {model} {name} spare part", QueryTier.VehiclePart));
        }

        if (!string.IsNullOrWhiteSpace(part.CompatibleMakes) && !string.IsNullOrWhiteSpace(part.CompatibleModels))
        {
            var compatMake = FirstValue(part.CompatibleMakes);
            var compatModel = FirstValue(part.CompatibleModels);
            var years = part.YearFrom is not null || part.YearTo is not null
                ? $"{part.YearFrom}-{part.YearTo}".Trim('-')
                : "";
            queries.Add(new SearchQuery($"{compatMake} {compatModel} {years} {name}", QueryTier.VehiclePart));
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            queries.Add(new SearchQuery($"{brand} {name} automotive", QueryTier.BrandPart));
            foreach (var id in part.ExternalIdentifiers)
            {
                queries.Add(new SearchQuery($"{brand} {id} auto part image", QueryTier.ExactIdentifier));
            }
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            queries.Add(new SearchQuery($"{make} {model} {year} {category} {name} spare part", QueryTier.VehiclePart));
        }

        queries.Add(new SearchQuery($"{name} automotive spare part", QueryTier.Generic));

        return queries
            .Select(q => q with { Text = CollapseWhitespace(q.Text) })
            .Where(q => !string.IsNullOrWhiteSpace(q.Text))
            .DistinctBy(q => q.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Clean(string? value) => value?.Trim() ?? "";

    private static string FirstValue(string value)
    {
        return value.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? value;
    }

    private static string CollapseWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}

internal sealed partial class ImageSearchClient
{
    private readonly HttpClient _http;
    private readonly EnrichmentOptions _options;
    private readonly RunLogger _logger;
    private readonly Dictionary<string, IReadOnlyList<ImageCandidate>> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<TextSearchResult>> _textCache = new(StringComparer.OrdinalIgnoreCase);

    public ImageSearchClient(HttpClient http, EnrichmentOptions options, RunLogger logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ImageCandidate>> SearchAsync(string query)
    {
        if (_imageCache.TryGetValue(query, out var cached))
        {
            return cached;
        }

        var candidates = new List<ImageCandidate>();

        if (!string.IsNullOrWhiteSpace(_options.GoogleApiKey) && !string.IsNullOrWhiteSpace(_options.GoogleCx))
        {
            candidates.AddRange(await SearchGoogleCseAsync(query));
        }

        // AutoZone direct search runs first when catalog extraction is enabled — it gives
        // real product images that pair with the trusted-domain High-confidence scoring.
        if (_options.UseCatalogPageExtraction)
        {
            candidates.AddRange(await SearchAutoZoneDirectAsync(query));
            candidates.AddRange(await SearchCatalogPageImagesAsync(query));
        }

        candidates.AddRange(await SearchOpenverseAsync(query));
        candidates.AddRange(await SearchWikimediaAsync(query));
        if (_options.UseBingTextSearch)
        {
            candidates.AddRange(await SearchBingImagesAsync(query));
        }

        var result = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c.ImageUrl))
            .DistinctBy(c => c.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToList();
        _imageCache[query] = result;
        return result;
    }

    /// <summary>Fetches raw HTML from any URL using the shared HttpClient with a browser-like
    /// User-Agent. Used by WebCatalogHarvester to query OEM catalog sites directly
    /// (e.g. partsouq.com/en/search?...) without going through Bing.</summary>
    public async Task<string?> FetchRawHtmlAsync(string url)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Use a browser UA to avoid anti-bot 403 responses from catalog sites
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn($"FetchRawHtmlAsync '{url}': HTTP {(int)response.StatusCode}");
                return null;
            }
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.Warn($"FetchRawHtmlAsync '{url}': {ex.Message}");
            return null;
        }
    }

    /// <summary>Bing-only text search, bypassing DuckDuckGo. Used by the web catalog harvester
    /// because DuckDuckGo times out consistently in this network environment.</summary>
    public async Task<IReadOnlyList<TextSearchResult>> SearchBingOnlyAsync(string query)
    {
        var cacheKey = $"BING:{query}";
        if (_textCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var results = (await SearchBingTextAsync(query))
            .Where(r => !string.IsNullOrWhiteSpace(r.Url))
            .DistinctBy(r => r.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _textCache[cacheKey] = results;
        return results;
    }

    public async Task<IReadOnlyList<TextSearchResult>> SearchTextAsync(string query)
    {
        if (_textCache.TryGetValue(query, out var cached))
        {
            return cached;
        }

        var results = new List<TextSearchResult>();

        if (!string.IsNullOrWhiteSpace(_options.GoogleApiKey) && !string.IsNullOrWhiteSpace(_options.GoogleCx))
        {
            results.AddRange(await SearchGoogleCseTextAsync(query));
        }

        if (_options.UseBingTextSearch)
        {
            results.AddRange(await SearchBingTextAsync(query));
        }

        if (_options.UseDuckDuckGoTextSearch)
        {
            results.AddRange(await SearchDuckDuckGoTextAsync(query));
        }

        var result = results
            .Where(r => !string.IsNullOrWhiteSpace(r.Url))
            .DistinctBy(r => r.Url, StringComparer.OrdinalIgnoreCase)
            .Take(_options.MaxTextResultsPerQuery)
            .ToList();
        _textCache[query] = result;
        return result;
    }

    private async Task<IReadOnlyList<ImageCandidate>> SearchOpenverseAsync(string query)
    {
        var url = "https://api.openverse.org/v1/images/" +
                  $"?q={Uri.EscapeDataString(query)}&page_size=8&format=json&mature=false";
        try
        {
            using var doc = await GetJsonAsync(url);
            if (doc is null || !doc.RootElement.TryGetProperty("results", out var results))
            {
                return [];
            }

            var candidates = new List<ImageCandidate>();
            foreach (var item in results.EnumerateArray())
            {
                var imageUrl = GetString(item, "url") ?? GetString(item, "thumbnail");
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var pageUrl = GetString(item, "foreign_landing_url");
                var license = GetString(item, "license");
                var title = GetString(item, "title") ?? "";
                candidates.Add(new ImageCandidate(
                    imageUrl,
                    GetString(item, "thumbnail") ?? imageUrl,
                    pageUrl,
                    Program.DomainFromUrl(pageUrl) is { Length: > 0 } d ? d : Program.DomainFromUrl(imageUrl),
                    title,
                    "Openverse",
                    license,
                    !string.IsNullOrWhiteSpace(license),
                    false));
            }

            return candidates;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Openverse search failed for '{query}': {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<ImageCandidate>> SearchWikimediaAsync(string query)
    {
        var url = "https://commons.wikimedia.org/w/api.php" +
                  "?action=query&generator=search&gsrnamespace=6&gsrlimit=8" +
                  "&prop=imageinfo&iiprop=url|mime|size|extmetadata&format=json" +
                  $"&gsrsearch={Uri.EscapeDataString(query)}";
        try
        {
            using var doc = await GetJsonAsync(url);
            if (doc is null ||
                !doc.RootElement.TryGetProperty("query", out var queryNode) ||
                !queryNode.TryGetProperty("pages", out var pages))
            {
                return [];
            }

            var candidates = new List<ImageCandidate>();
            foreach (var page in pages.EnumerateObject().Select(p => p.Value))
            {
                var title = GetString(page, "title") ?? "";
                if (!page.TryGetProperty("imageinfo", out var infoArray) || infoArray.GetArrayLength() == 0)
                {
                    continue;
                }

                var info = infoArray[0];
                var imageUrl = GetString(info, "url");
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var mime = GetString(info, "mime");
                if (mime?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true ||
                    !LooksLikeUsefulImageUrl(imageUrl))
                {
                    continue;
                }

                var license = "Wikimedia Commons";
                if (info.TryGetProperty("extmetadata", out var meta) &&
                    meta.TryGetProperty("LicenseShortName", out var licenseNode) &&
                    licenseNode.TryGetProperty("value", out var licenseValue))
                {
                    license = licenseValue.GetString() ?? license;
                }

                var pageUrl = $"https://commons.wikimedia.org/wiki/{Uri.EscapeDataString(title.Replace(' ', '_'))}";
                candidates.Add(new ImageCandidate(
                    imageUrl,
                    imageUrl,
                    pageUrl,
                    "commons.wikimedia.org",
                    title.Replace("File:", "", StringComparison.OrdinalIgnoreCase).Replace('_', ' '),
                    "Wikimedia",
                    license,
                    true,
                    false));
            }

            return candidates;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Wikimedia search failed for '{query}': {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<ImageCandidate>> SearchGoogleCseAsync(string query)
    {
        var url = "https://www.googleapis.com/customsearch/v1" +
                  $"?key={Uri.EscapeDataString(_options.GoogleApiKey)}" +
                  $"&cx={Uri.EscapeDataString(_options.GoogleCx)}" +
                  $"&q={Uri.EscapeDataString(query)}&searchType=image&num=8&safe=active";
        try
        {
            using var doc = await GetJsonAsync(url);
            if (doc is null || !doc.RootElement.TryGetProperty("items", out var items))
            {
                return [];
            }

            var candidates = new List<ImageCandidate>();
            foreach (var item in items.EnumerateArray())
            {
                var imageUrl = GetString(item, "link");
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var pageUrl = item.TryGetProperty("image", out var imageNode)
                    ? GetString(imageNode, "contextLink")
                    : null;
                var domain = Program.DomainFromUrl(pageUrl);
                var allowedCatalog = _options.AllowedCatalogDomains.Count == 0 ||
                                     _options.AllowedCatalogDomains.Contains(domain);
                if (!allowedCatalog)
                {
                    continue;
                }

                candidates.Add(new ImageCandidate(
                    imageUrl,
                    item.TryGetProperty("image", out imageNode) ? GetString(imageNode, "thumbnailLink") : imageUrl,
                    pageUrl,
                    string.IsNullOrWhiteSpace(domain) ? Program.DomainFromUrl(imageUrl) : domain,
                    GetString(item, "title") ?? "",
                    "GoogleCSE",
                    "unknown",
                    false,
                    true));
            }

            return candidates;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Google CSE search failed for '{query}': {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<ImageCandidate>> SearchBingImagesAsync(string query)
    {
        var url = $"https://www.bing.com/images/search?q={Uri.EscapeDataString(query)}&setlang=en-US";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var html = await response.Content.ReadAsStringAsync();
            var candidates = new List<ImageCandidate>();
            foreach (Match match in BingImageMetadataRegex().Matches(html))
            {
                var metadata = WebUtility.HtmlDecode(match.Groups["metadata"].Value);
                var imageUrl = ExtractJsonLikeValue(metadata, "murl");
                if (!LooksLikeUsefulImageUrl(imageUrl))
                {
                    continue;
                }

                var thumbUrl = ExtractJsonLikeValue(metadata, "turl");
                var pageUrl = ExtractJsonLikeValue(metadata, "purl");
                var title = ExtractJsonLikeValue(metadata, "t");
                candidates.Add(new ImageCandidate(
                    imageUrl,
                    string.IsNullOrWhiteSpace(thumbUrl) ? imageUrl : thumbUrl,
                    string.IsNullOrWhiteSpace(pageUrl) ? null : pageUrl,
                    Program.DomainFromUrl(pageUrl) is { Length: > 0 } d ? d : Program.DomainFromUrl(imageUrl),
                    string.IsNullOrWhiteSpace(title) ? query : title,
                    "BingImages",
                    "unknown",
                    false,
                    true));

                if (candidates.Count >= Math.Max(3, _options.CandidateLimitPerPart + 1))
                {
                    break;
                }
            }

            return candidates;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Bing Images search failed for '{query}': {ex.Message}");
            return [];
        }
    }


    // AutoZone CDN image URL pattern (direct product images)
    [GeneratedRegex(@"https://contentassets\.autozone\.com/product_image/[^\s""'<>]+\.(?:jpg|jpeg|png|webp)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AutoZoneCdnImageRegex();

    private async Task<IReadOnlyList<ImageCandidate>> SearchAutoZoneDirectAsync(string query)
    {
        var searchUrl = $"https://www.autozone.com/searchresult?searchText={Uri.EscapeDataString(query)}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var html = await response.Content.ReadAsStringAsync();
            var pageUri = new Uri(searchUrl);
            var candidates = new List<ImageCandidate>();

            // Extract AutoZone CDN product images directly (e.g. contentassets.autozone.com)
            foreach (Match match in AutoZoneCdnImageRegex().Matches(html))
            {
                var imageUrl = match.Value.Trim('"', '\'');
                if (!LooksLikeUsefulImageUrl(imageUrl))
                {
                    continue;
                }

                candidates.Add(new ImageCandidate(
                    imageUrl,
                    imageUrl,
                    searchUrl,
                    "autozone.com",
                    query,
                    "CatalogPage",
                    "unknown",
                    false,
                    true));

                if (candidates.Count >= 3)
                {
                    break;
                }
            }

            // Fall back to og:image / standard img extraction
            if (candidates.Count == 0)
            {
                var textResult = new TextSearchResult(query, searchUrl, "", "AutoZone");
                candidates.AddRange(ExtractPageImages(pageUri, html, textResult).Take(3));
            }

            return candidates
                .DistinctBy(c => c.ImageUrl, StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Warn($"AutoZone direct search failed for '{query}': {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<ImageCandidate>> SearchCatalogPageImagesAsync(string query)
    {
        var textResults = await SearchTextAsync($"{query} image");
        var candidates = new List<ImageCandidate>();
        foreach (var result in textResults.Take(3))
        {
            if (!Uri.TryCreate(result.Url, UriKind.Absolute, out var pageUri) ||
                (pageUri.Scheme != Uri.UriSchemeHttps && pageUri.Scheme != Uri.UriSchemeHttp))
            {
                continue;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, pageUri);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode ||
                    response.Content.Headers.ContentType?.MediaType?.Contains("html", StringComparison.OrdinalIgnoreCase) != true)
                {
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync();
                candidates.AddRange(ExtractPageImages(pageUri, html, result));
            }
            catch (Exception ex)
            {
                _logger.Warn($"Catalog page image extraction failed for '{result.Url}': {ex.Message}");
            }
        }

        return candidates
            .DistinctBy(c => c.ImageUrl, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static IEnumerable<ImageCandidate> ExtractPageImages(Uri pageUri, string html, TextSearchResult result)
    {
        foreach (Match match in OgImageRegex().Matches(html))
        {
            var imageUrl = ResolveImageUrl(pageUri, WebUtility.HtmlDecode(match.Groups["url"].Value));
            if (!LooksLikeUsefulImageUrl(imageUrl))
            {
                continue;
            }

            yield return new ImageCandidate(
                imageUrl,
                imageUrl,
                pageUri.ToString(),
                pageUri.Host.ToLowerInvariant(),
                result.Title,
                "CatalogPage",
                "unknown",
                false,
                true);
        }

        foreach (Match match in ImgTagRegex().Matches(html))
        {
            var imageUrl = ResolveImageUrl(pageUri, WebUtility.HtmlDecode(match.Groups["url"].Value));
            if (!LooksLikeUsefulImageUrl(imageUrl))
            {
                continue;
            }

            var alt = StripTags(WebUtility.HtmlDecode(match.Groups["alt"].Value));
            yield return new ImageCandidate(
                imageUrl,
                imageUrl,
                pageUri.ToString(),
                pageUri.Host.ToLowerInvariant(),
                string.Join(" ", new[] { result.Title, alt }.Where(s => !string.IsNullOrWhiteSpace(s))),
                "CatalogPage",
                "unknown",
                false,
                true);
        }
    }

    private async Task<IReadOnlyList<TextSearchResult>> SearchGoogleCseTextAsync(string query)
    {
        var url = "https://www.googleapis.com/customsearch/v1" +
                  $"?key={Uri.EscapeDataString(_options.GoogleApiKey)}" +
                  $"&cx={Uri.EscapeDataString(_options.GoogleCx)}" +
                  $"&q={Uri.EscapeDataString(query)}&num={Math.Min(_options.MaxTextResultsPerQuery, 10)}&safe=active";
        try
        {
            using var doc = await GetJsonAsync(url);
            if (doc is null || !doc.RootElement.TryGetProperty("items", out var items))
            {
                return [];
            }

            var results = new List<TextSearchResult>();
            foreach (var item in items.EnumerateArray())
            {
                var link = GetString(item, "link");
                if (string.IsNullOrWhiteSpace(link))
                {
                    continue;
                }

                results.Add(new TextSearchResult(
                    GetString(item, "title") ?? "",
                    link,
                    GetString(item, "snippet") ?? "",
                    "GoogleCSE"));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Google CSE text search failed for '{query}': {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<TextSearchResult>> SearchBingTextAsync(string query)
    {
        var url = $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}&setlang=en-US";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var html = await response.Content.ReadAsStringAsync();
            var results = new List<TextSearchResult>();
            foreach (var block in BingBlockRegex().Split(html).Skip(1))
            {
                var anchor = BingAnchorRegex().Match(block);
                if (!anchor.Success)
                {
                    continue;
                }

                var link = WebUtility.HtmlDecode(anchor.Groups["url"].Value);
                var title = StripTags(WebUtility.HtmlDecode(anchor.Groups["title"].Value));
                var snippetMatch = BingParagraphRegex().Match(block);
                var snippet = snippetMatch.Success
                    ? StripTags(WebUtility.HtmlDecode(snippetMatch.Groups["snippet"].Value))
                    : "";
                if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                results.Add(new TextSearchResult(title, link, snippet, "Bing"));
                if (results.Count >= _options.MaxTextResultsPerQuery)
                {
                    break;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Bing text search failed for '{query}': {ex.Message}");
            return [];
        }
    }


    private async Task<IReadOnlyList<TextSearchResult>> SearchDuckDuckGoTextAsync(string query)
    {
        var url = $"https://duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var html = await response.Content.ReadAsStringAsync();
            var results = new List<TextSearchResult>();
            foreach (Match match in DuckResultRegex().Matches(html))
            {
                var rawUrl = WebUtility.HtmlDecode(match.Groups["url"].Value);
                var title = StripTags(WebUtility.HtmlDecode(match.Groups["title"].Value));
                var snippet = "";
                var after = html[Math.Min(match.Index + match.Length, html.Length)..];
                var snippetMatch = DuckSnippetRegex().Match(after);
                if (snippetMatch.Success)
                {
                    snippet = StripTags(WebUtility.HtmlDecode(snippetMatch.Groups["snippet"].Value));
                }

                var decodedUrl = DecodeDuckDuckGoUrl(rawUrl);
                if (string.IsNullOrWhiteSpace(decodedUrl))
                {
                    continue;
                }

                results.Add(new TextSearchResult(title, decodedUrl, snippet, "DuckDuckGo"));
                if (results.Count >= _options.MaxTextResultsPerQuery)
                {
                    break;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.Warn($"DuckDuckGo text search failed for '{query}': {ex.Message}");
            return [];
        }
    }

    private async Task<JsonDocument?> GetJsonAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string DecodeDuckDuckGoUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return "";
        }

        if (rawUrl.StartsWith("//duckduckgo.com/l/?", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.StartsWith("https://duckduckgo.com/l/?", StringComparison.OrdinalIgnoreCase))
        {
            var absolute = rawUrl.StartsWith("//", StringComparison.Ordinal) ? "https:" + rawUrl : rawUrl;
            if (Uri.TryCreate(absolute, UriKind.Absolute, out var uri))
            {
                var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in query)
                {
                    var parts = pair.Split('=', 2);
                    if (parts.Length == 2 && parts[0].Equals("uddg", StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(parts[1].Replace("+", " ", StringComparison.Ordinal));
                    }
                }
            }
        }

        return rawUrl;
    }

    private static string StripTags(string value)
    {
        var withoutTags = Regex.Replace(value, "<.*?>", " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    private static string ResolveImageUrl(Uri pageUri, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        if (raw.StartsWith("//", StringComparison.Ordinal))
        {
            return $"{pageUri.Scheme}:{raw}";
        }

        return Uri.TryCreate(pageUri, raw, out var resolved) ? resolved.ToString() : raw;
    }

    private static bool LooksLikeUsefulImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var lowered = url.ToLowerInvariant();
        if (lowered.Contains("logo") ||
            lowered.Contains("icon") ||
            lowered.Contains("sprite") ||
            lowered.Contains("placeholder") ||
            lowered.Contains("no-image") ||
            lowered.Contains("tracking") ||
            lowered.Contains("pixel") ||
            lowered.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath.ToLowerInvariant();
        return path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
               lowered.Contains("image") ||
               lowered.Contains("photo") ||
               lowered.Contains("product");
    }

    private static string ExtractJsonLikeValue(string metadata, string name)
    {
        var match = Regex.Match(
            metadata,
            $"\"{Regex.Escape(name)}\"\\s*:\\s*\"(?<value>(?:\\\\\"|[^\"])*)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return "";
        }

        return Regex.Unescape(match.Groups["value"].Value);
    }

    [GeneratedRegex("<a[^>]+class=\"result__a\"[^>]+href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex DuckResultRegex();

    [GeneratedRegex("<a[^>]+class=\"result__snippet\"[^>]*>(?<snippet>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex DuckSnippetRegex();

    [GeneratedRegex("<li\\s+class=\"b_algo\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BingBlockRegex();

    [GeneratedRegex("<h2[^>]*>.*?<a[^>]+href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>.*?</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BingAnchorRegex();

    [GeneratedRegex("<p[^>]*>(?<snippet>.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BingParagraphRegex();

    [GeneratedRegex("m=\"(?<metadata>\\{.*?&quot;murl&quot;.*?\\})\"", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BingImageMetadataRegex();

    [GeneratedRegex("<meta[^>]+(?:property|name)=[\"'](?:og:image|twitter:image)[\"'][^>]+content=[\"'](?<url>[^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex OgImageRegex();

    [GeneratedRegex("<img[^>]+(?:src|data-src|data-original)=[\"'](?<url>[^\"']+)[\"'][^>]*(?:alt=[\"'](?<alt>[^\"']*)[\"'])?[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ImgTagRegex();
}
