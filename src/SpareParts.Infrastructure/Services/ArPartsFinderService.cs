using SpareParts.Domain.Ar;

namespace SpareParts.Infrastructure.Services;

public sealed class ArPartsFinderService
{
    private readonly SmartSearchService _smartSearch;

    public ArPartsFinderService(SmartSearchService smartSearch)
    {
        _smartSearch = smartSearch;
    }

    public ArScanResult Scan(ArScanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhotoUrl) && string.IsNullOrWhiteSpace(request.Make))
        {
            return new ArScanResult
            {
                Success = false,
                Confidence = "0%",
                Disclaimer = "Please provide a photo URL or vehicle information."
            };
        }

        var searchQuery = BuildSearchQuery(request);
        var searchResult = _smartSearch.Search(searchQuery, 8);
        var partResults = searchResult.Results
            .Where(r => r.EntityType == "Part")
            .Take(5)
            .ToList();

        if (partResults.Count == 0)
        {
            return new ArScanResult
            {
                Success = false,
                Confidence = "Low",
                Disclaimer = "No matching parts found in the catalog."
            };
        }

        var matched = partResults.Select(r => new ArMatchedPart
        {
            PartId = r.Id,
            PartName = r.Title,
            InternalCode = r.Subtitle
        }).ToList();

        return new ArScanResult
        {
            Success = true,
            MatchedParts = matched,
            Confidence = matched.Count >= 3 ? "High" : "Medium",
            Disclaimer = "AR matching is in preview. Results are based on catalog search, not image recognition."
        };
    }

    private static string BuildSearchQuery(ArScanRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Make)) parts.Add(request.Make);
        if (!string.IsNullOrWhiteSpace(request.Model)) parts.Add(request.Model);
        if (request.Year.HasValue) parts.Add(request.Year.Value.ToString());
        return string.Join(" ", parts);
    }
}
