using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface ISubscriptionService
    {
        TenantSubscriptionDto GetCurrent(int tenantId);

        TenantSubscriptionDto StartFree(int tenantId, int userId);

        TenantSubscriptionDto StartTrial(int tenantId, StartTrialRequest request, int userId);

        Task<CheckoutResponseDto> CheckoutAsync(int tenantId, CheckoutRequest request, int userId, CancellationToken cancellationToken);

        TenantSubscriptionDto ChangePlan(int tenantId, ChangePlanRequest request, int userId);

        void Cancel(int tenantId, CancelSubscriptionRequest request, int userId);

        IReadOnlyList<TenantSubscriptionDto> GetAllForAdmin();

        TenantSubscriptionDto AdminActivate(AdminActivateSubscriptionRequest request, int userId);

        /// <summary>Expires due subscriptions, marks past-due ones, and resets monthly usage counters. Returns the number of subscriptions changed.</summary>
        int RunMaintenance();
    }
}
