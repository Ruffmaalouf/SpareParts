namespace SpareParts.Infrastructure.Services
{
    public sealed class DisabledCommunicationDeliveryClient : ICommunicationDeliveryClient
    {
        public Task<CommunicationDeliveryResult> SendAsync(
            CommunicationPreparedMessage message,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CommunicationDeliveryResult
            {
                Status = "Prepared",
                Provider = "Disabled",
                ProviderStatus = "NotConfigured",
                ErrorMessage = "Communication webhook is not configured; message was prepared and logged only."
            });
        }
    }
}
