using System.Globalization;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services;

internal static class TransactionSerialNumberFormatter
{
    private const string NumberToken = "{NUMBER";
    private static readonly Regex TokenRegex = new(@"\{(?<token>NUMBER|DATE|TYPEKEY|NAME)(:(?<format>[^{}]+))?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Format(string template, DateTime nowUtc, long number, string typeKey, string name)
    {
        var normalizedTemplate = NormalizeTemplate(template);

        try
        {
            return TokenRegex.Replace(normalizedTemplate, match =>
            {
                var token = match.Groups["token"].Value.ToUpperInvariant();
                var tokenFormat = match.Groups["format"].Success
                    ? match.Groups["format"].Value
                    : string.Empty;

                return token switch
                {
                    "NUMBER" => string.IsNullOrWhiteSpace(tokenFormat)
                        ? number.ToString(CultureInfo.InvariantCulture)
                        : number.ToString(tokenFormat, CultureInfo.InvariantCulture),
                    "DATE" => nowUtc.ToString(
                        string.IsNullOrWhiteSpace(tokenFormat) ? "yyyyMMdd" : tokenFormat,
                        CultureInfo.InvariantCulture),
                    "TYPEKEY" => typeKey,
                    "NAME" => name,
                    _ => match.Value
                };
            });
        }
        catch (FormatException)
        {
            throw new ValidationException("Serial number format contains an invalid DATE or NUMBER format.");
        }
    }

    public static void Validate(string template)
    {
        var normalizedTemplate = NormalizeTemplate(template);
        if (!normalizedTemplate.Contains(NumberToken, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Serial number format must include a {NUMBER} token.");
        }

        _ = Format(normalizedTemplate, new DateTime(2026, 4, 21), 123, "sample_type", "Sample Type");
    }

    private static string NormalizeTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ValidationException("Serial number format is required.");
        }

        return template.Trim();
    }
}
