namespace SpareParts.Desktop.Wpf
{
    public sealed class ApiErrorEnvelope
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? TraceId { get; set; }
    }

    public class ApiClientException : Exception
    {
        public ApiClientException(string code, string message, string? traceId = null) : base(message)
        {
            Code = code;
            TraceId = traceId;
        }

        public string Code { get; }
        public string? TraceId { get; }
    }
}
