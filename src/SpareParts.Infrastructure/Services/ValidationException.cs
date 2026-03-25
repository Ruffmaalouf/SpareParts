namespace SpareParts.Infrastructure.Services
{
    public sealed class ValidationException : DomainException
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}
