using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class SalesReturnsService
    {
        private readonly CreateSalesReturnHandler _handler;
        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public SalesReturnsService(
            CreateSalesReturnHandler handler,
            ISqlConnectionFactory factory,
            ITenantContext tenantContext)
        {
            _handler = handler;
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public CreateSalesReturnResponse CreateReturn(CreateSalesReturnRequest request, int userId)
            => _handler.Handle(request, userId);

        public List<SalesReturnLookupDto> SearchReturns(string? query)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repo = new SalesReturnRepository(session);
            return repo.SearchReturns(query);
        }

        public SalesReturnDetailsDto? GetReturnById(int returnId)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repo = new SalesReturnRepository(session);
            return repo.GetReturnById(returnId);
        }

        public List<ReturnableLineDto> GetReturnableLines(int originalInvoiceId)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repo = new SalesReturnRepository(session);
            return repo.GetReturnableLines(originalInvoiceId);
        }
    }
}
