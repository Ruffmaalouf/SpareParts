namespace SpareParts.Infrastructure.Services;

public sealed class ExternalServiceException(string message) : DomainException(message)
{
}
