using System;

namespace SpareParts.Desktop.Wpf
{
    public sealed class DomainValidationException : Exception
    {
        public DomainValidationException(string message, string code = "validation_error") : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
