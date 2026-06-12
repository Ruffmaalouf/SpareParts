using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IPaymentService
    {
        IReadOnlyList<PaymentDto> GetHistory(int tenantId);

        PaymentDto GetById(int tenantId, int id);

        Task<WebhookResult> HandleWebhookAsync(string providerCode, WebhookRequestContext context, CancellationToken cancellationToken);

        IReadOnlyList<PaymentDto> GetAllForAdmin();

        IReadOnlyList<WebhookEventDto> GetWebhookEventsForAdmin();

        PaymentDto MarkPaid(int paymentId, MarkPaymentPaidRequest request, int userId);
    }
}
