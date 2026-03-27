using SpareParts.Domain.Common;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IPaymentStatusPolicy
    {
        PaymentStatus Resolve(decimal totalAmount, decimal paidAmount);
    }
}
