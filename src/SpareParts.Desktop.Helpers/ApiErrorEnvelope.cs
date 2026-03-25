namespace SpareParts.Desktop.Wpf
{
    public sealed class ApiErrorEnvelope
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? TraceId { get; set; }
    }
}
