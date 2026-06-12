using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IInvoiceService
    {
        IReadOnlyList<InvoiceDto> GetAll(int tenantId);

        InvoiceDto GetById(int tenantId, int id);

        int CreateForPayment(int paymentId, int? userId);

        IReadOnlyList<InvoiceDto> GetAllForAdmin();
    }
}
