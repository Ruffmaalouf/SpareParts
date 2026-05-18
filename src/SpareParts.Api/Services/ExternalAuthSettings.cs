namespace SpareParts.Api.Services;

public sealed class ExternalAuthSettings
{
    public string GoogleClientId { get; set; } = string.Empty;
    public string FacebookAppId { get; set; } = string.Empty;
    public string FacebookAppSecret { get; set; } = string.Empty;
}
