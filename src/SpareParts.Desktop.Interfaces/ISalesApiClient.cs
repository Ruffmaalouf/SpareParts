using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface ISalesApiClient
    {
        Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req);
        Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query);
        Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId);
    }
}
