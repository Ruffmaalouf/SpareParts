using SpareParts.Domain.Common;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class DefaultPaymentStatusPolicy : IPaymentStatusPolicy
    {
        public PaymentStatus Resolve(decimal totalAmount, decimal paidAmount)
        {
            if (paidAmount <= 0)
            {
                return PaymentStatus.Unpaid;
            }

            if (paidAmount >= totalAmount)
            {
                return PaymentStatus.Paid;
            }

            return PaymentStatus.PartiallyPaid;
        }
    }
}
