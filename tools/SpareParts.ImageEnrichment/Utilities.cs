using System.Text;
using System.Text.Json;

namespace SpareParts.ImageEnrichment;

internal sealed class RunLogger : IDisposable
{
    private readonly StreamWriter _writer;

    public RunLogger(PathInfo path)
    {
        _writer = new StreamWriter(path.FullName, append: false, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:HH:mm:ss} {level,-5} {message}";
        Console.WriteLine(line);
        _writer.WriteLine(line);
    }

    public void Dispose() => _writer.Dispose();
}

internal sealed class ProgressState
{
    public HashSet<int> ProcessedPartIds { get; set; } = [];
    public int? LastProcessedPartId { get; set; }
    public DateTimeOffset? LastRunAtUtc { get; set; }

    public static ProgressState Load(PathInfo path)
    {
        try
        {
            var json = File.ReadAllText(path.FullName);
            return JsonSerializer.Deserialize<ProgressState>(json) ?? new ProgressState();
        }
        catch
        {
            return new ProgressState();
        }
    }

    public async Task SaveAsync(PathInfo path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path.FullName, json);
    }
}

internal static class CsvWriter
{
    public static async Task WriteObjectsAsync<T>(PathInfo path, IReadOnlyList<T> rows)
    {
        var dictionaries = rows.Select(ToDictionary).ToList();
        await WriteDictionariesAsync(path, dictionaries);
    }

    public static async Task WriteDictionariesAsync(PathInfo path, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        await using var writer = new StreamWriter(path.FullName, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        if (rows.Count == 0)
        {
            return;
        }

        var headers = rows.SelectMany(r => r.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        await writer.WriteLineAsync(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
        {
            var values = headers.Select(h => row.TryGetValue(h, out var value) ? value : null);
            await writer.WriteLineAsync(string.Join(",", values.Select(v => Escape(v?.ToString() ?? ""))));
        }
    }

    private static Dictionary<string, object?> ToDictionary<T>(T item)
    {
        if (item is IReadOnlyDictionary<string, object?> ro)
        {
            return ro.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        if (item is IDictionary<string, object?> dict)
        {
            return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(T).GetProperties())
        {
            result[prop.Name] = prop.GetValue(item);
        }

        return result;
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}

internal static class CsvReader
{
    public static IReadOnlyList<Dictionary<string, string>> Read(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length == 0)
        {
            return [];
        }

        var headers = ParseLine(lines[0]).ToArray();
        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseLine(line).ToArray();
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                row[headers[i]] = i < values.Length ? values[i] : "";
            }

            rows.Add(row);
        }

        return rows;
    }

    private static IEnumerable<string> ParseLine(string line)
    {
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        yield return sb.ToString();
    }
}

