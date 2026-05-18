namespace SpareParts.Domain.Auth
{
    public sealed class ExternalLoginRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string? IdToken { get; set; }
        public string? AccessToken { get; set; }
    }
}
