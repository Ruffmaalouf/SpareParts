using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ISalesReturnRepository
    {
        int InsertReturn(SalesReturn salesReturn);
        void InsertItems(int returnId, IList<SalesInvoiceItem> items);
        bool ReturnNumberExists(string returnNumber);
        List<SalesReturnLookupDto> SearchReturns(string? query);
        SalesReturnDetailsDto? GetReturnById(int returnId);
        List<ReturnableLineDto> GetReturnableLines(int originalInvoiceId);
    }
}
