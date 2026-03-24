namespace SpareParts.Api.Errors
{
    public sealed class ApiErrorEnvelope
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? TraceId { get; init; }
    }
}
