using System.Collections.Generic;
using System.Threading.Tasks;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Client for the new client-workspace endpoints (Ignition redesign, Report 04 §05–§06):
    /// GET /api/clients/{id}/workspace plus the paged child routes and the typeahead search.
    /// Default page size is 25, max 100, per the documented contract.
    /// </summary>
    public interface IClientWorkspaceApiClient
    {
        Task<IgnitionClientWorkspaceDto> GetWorkspaceAsync(int customerId);

        Task<List<IgnitionClientSearchResultDto>> SearchClientsAsync(string query, int take = 10);

        Task<List<IgnitionClientInvoiceRowDto>> GetInvoicesAsync(int customerId, int page = 1, int pageSize = 25, string? sort = null, string? status = null);

        Task<List<IgnitionClientPaymentRowDto>> GetPaymentsAsync(int customerId, int page = 1, int pageSize = 25, string? sort = null);

        Task<List<QuoteLookupDto>> GetQuotesAsync(int customerId, int page = 1, int pageSize = 25, string? status = null);

        Task<List<PartRequestDto>> GetPartRequestsAsync(int customerId, int page = 1, int pageSize = 25, string? status = null);

        Task<List<IgnitionClientTimelineEventDto>> GetTimelineAsync(int customerId, int page = 1, int pageSize = 25, string? types = null);
    }
}
