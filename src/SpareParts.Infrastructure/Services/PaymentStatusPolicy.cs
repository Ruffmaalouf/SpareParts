namespace SpareParts.Infrastructure.Services
{
    public interface IPaymentStatusPolicy
    {
        string Resolve(decimal totalAmount, decimal paidAmount);
    }

    public class DefaultPaymentStatusPolicy : IPaymentStatusPolicy
    {
        public string Resolve(decimal totalAmount, decimal paidAmount)
        {
            if (paidAmount <= 0)
            {
                return "Unpaid";
            }

            if (paidAmount >= totalAmount)
            {
                return "Paid";
            }

            return "PartiallyPaid";
        }
    }
}
