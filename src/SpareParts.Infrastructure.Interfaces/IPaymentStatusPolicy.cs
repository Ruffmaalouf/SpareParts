namespace SpareParts.Infrastructure.Interfaces
{
    public interface IPaymentStatusPolicy
    {
        string Resolve(decimal totalAmount, decimal paidAmount);
    }
}
