namespace SpareParts.Infrastructure.Services
{
    public interface ICommunicationDeliveryClient
    {
        Task<CommunicationDeliveryResult> SendAsync(
            CommunicationPreparedMessage message,
            CancellationToken cancellationToken);
    }
}
