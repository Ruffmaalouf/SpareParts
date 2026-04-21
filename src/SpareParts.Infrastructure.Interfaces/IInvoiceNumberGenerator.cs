namespace SpareParts.Infrastructure.Interfaces
{
    public interface IInvoiceNumberGenerator
    {
        string NextSalesNumber();
        string NextPurchaseNumber();
        string NextUsedCarPurchaseNumber();
    }
}
