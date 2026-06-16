using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public sealed class SalesReturnsService
    {
        private readonly CreateSalesReturnHandler _handler;
        private readonly ISqlConnectionFactory _factory;

        public SalesReturnsService(
            CreateSalesReturnHandler handler,
            ISqlConnectionFactory factory)
        {
            _handler = handler;
            _factory = factory;
        }

        public CreateSalesReturnResponse CreateReturn(CreateSalesReturnRequest request, int userId)
            => _handler.Handle(request, userId);

        public List<SalesReturnLookupDto> SearchReturns(string? query)
        {
            using var session = new DbSession(_factory);
            var repo = new SalesReturnRepository(session);
            return repo.SearchReturns(query);
        }

        public SalesReturnDetailsDto? GetReturnById(int returnId)
        {
            using var session = new DbSession(_factory);
            var repo = new SalesReturnRepository(session);
            return repo.GetReturnById(returnId);
        }

        public List<ReturnableLineDto> GetReturnableLines(int originalInvoiceId)
        {
            using var session = new DbSession(_factory);
            var repo = new SalesReturnRepository(session);
            return repo.GetReturnableLines(originalInvoiceId);
        }
    }
}
