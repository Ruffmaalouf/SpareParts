namespace SpareParts.Infrastructure.Services;

public sealed class OpenAiOptions
{
    public string? ApiKey { get; init; }
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
    public string Model { get; init; } = "gpt-5-mini";
    public int TimeoutSeconds { get; init; } = 30;
}
