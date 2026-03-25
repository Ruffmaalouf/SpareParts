using System;

namespace SpareParts.Desktop.Wpf
{
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
