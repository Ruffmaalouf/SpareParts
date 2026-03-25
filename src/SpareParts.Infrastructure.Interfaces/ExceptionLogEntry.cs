namespace SpareParts.Infrastructure.Data
{
    public class ExceptionLogEntry
    {
        public DateTime OccurredAtUtc { get; init; }
        public string Application { get; init; } = string.Empty;
        public string? TraceId { get; init; }
        public string? HttpMethod { get; init; }
        public string? RequestPath { get; init; }
        public int StatusCode { get; init; }
        public string ErrorCode { get; init; } = string.Empty;
        public string ExceptionType { get; init; } = string.Empty;
        public string ExceptionMessage { get; init; } = string.Empty;
        public string? SourceClass { get; init; }
        public string? SourceMethod { get; init; }
        public int? SourceLine { get; init; }
        public string? StackTrace { get; init; }
        public bool IsCaught { get; init; }
    }
}
