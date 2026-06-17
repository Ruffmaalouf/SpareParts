using SpareParts.Domain.Concierge;

namespace SpareParts.Infrastructure.Services;

public sealed class KareemConciergeService
{
    private readonly SmartSearchService _smartSearch;

    public KareemConciergeService(SmartSearchService smartSearch)
    {
        _smartSearch = smartSearch;
    }

    public KareemChatResponse Chat(KareemChatRequest request)
    {
        var msg = (request.Message ?? string.Empty).Trim();
        var lower = msg.ToLowerInvariant();
        var lang = request.Language ?? "en";

        var intent = DetectIntent(lower);

        return intent switch
        {
            "search_part" => HandlePartSearch(msg, lang),
            "price_check" => HandlePriceCheck(msg, lang),
            "greeting" => new KareemChatResponse
            {
                Reply = lang == "ar"
                    ? "مرحبا! أنا كريم، مساعدك في قطع الغيار. كيف يمكنني مساعدتك اليوم؟"
                    : "Hello! I'm Kareem, your spare parts assistant. How can I help you today?",
                Intent = "greeting",
                Language = lang,
                SuggestedActions = ["Search for a part", "Check prices", "Find compatible parts"]
            },
            "compatibility" => new KareemChatResponse
            {
                Reply = lang == "ar"
                    ? "يمكنني مساعدتك في التحقق من التوافق. أخبرني باسم القطعة والسيارة."
                    : "I can help you check compatibility. Tell me the part name and your vehicle details.",
                Intent = "compatibility",
                Language = lang,
                SuggestedActions = ["Check compatibility", "Browse catalog"]
            },
            _ => new KareemChatResponse
            {
                Reply = lang == "ar"
                    ? $"فهمت سؤالك عن '{msg}'. يمكنني مساعدتك في البحث عن القطع والأسعار والتوافق."
                    : $"I understood your question about '{msg}'. I can help you search parts, check prices, and verify compatibility.",
                Intent = "general",
                Language = lang,
                SuggestedActions = ["Search parts", "View catalog", "Contact seller"]
            }
        };
    }

    private KareemChatResponse HandlePartSearch(string msg, string lang)
    {
        var results = _smartSearch.Search(msg, 5);
        var partNames = results.Results
            .Where(r => r.EntityType == "Part")
            .Select(r => r.Title)
            .Take(3)
            .ToList();

        var reply = partNames.Count > 0
            ? (lang == "ar"
                ? $"وجدت هذه القطع: {string.Join("، ", partNames)}"
                : $"I found these parts: {string.Join(", ", partNames)}")
            : (lang == "ar"
                ? "لم أجد قطعًا مطابقة. جرب كلمات مختلفة."
                : "No matching parts found. Try different keywords.");

        return new KareemChatResponse
        {
            Reply = reply,
            Intent = "search_part",
            Language = lang,
            SuggestedActions = partNames.Count > 0 ? ["View part details", "Check price", "Add to watchlist"] : ["Try different search", "Browse catalog"]
        };
    }

    private KareemChatResponse HandlePriceCheck(string msg, string lang)
    {
        return new KareemChatResponse
        {
            Reply = lang == "ar"
                ? "للتحقق من السعر، يرجى الذهاب إلى صفحة قطعة الغيار أو استخدام ميزة PriceGenius."
                : "To check price, please visit the part page or use the PriceGenius feature for AI-powered suggestions.",
            Intent = "price_check",
            Language = lang,
            SuggestedActions = ["Use PriceGenius", "View market index"]
        };
    }

    private static string DetectIntent(string lower)
    {
        if (lower.Contains("hello") || lower.Contains("hi") || lower.Contains("مرحب") || lower.Contains("السلام"))
            return "greeting";
        if (lower.Contains("price") || lower.Contains("cost") || lower.Contains("how much") || lower.Contains("سعر") || lower.Contains("كم"))
            return "price_check";
        if (lower.Contains("compatible") || lower.Contains("fit") || lower.Contains("توافق") || lower.Contains("يناسب"))
            return "compatibility";
        if (lower.Contains("find") || lower.Contains("search") || lower.Contains("looking for") || lower.Contains("بحث") || lower.Contains("أبحث"))
            return "search_part";
        return "general";
    }
}
