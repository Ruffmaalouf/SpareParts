namespace SpareParts.Infrastructure.Interfaces
{
    public interface IInvoiceNumberGenerator
    {
        string NextSalesNumber();
        string NextSalesReturnNumber();
        string NextPurchaseNumber();
        string NextUsedCarPurchaseNumber();
    }
}
