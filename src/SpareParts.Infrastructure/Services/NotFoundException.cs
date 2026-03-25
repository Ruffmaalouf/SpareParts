namespace SpareParts.Infrastructure.Services
{
    public sealed class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
