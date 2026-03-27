using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;
using System.Collections.Generic;

namespace SpareParts.Infrastructure.Services
{
    public class SalesService
    {
        private readonly ICreateSaleHandler _createSaleHandler;
        private readonly ISqlConnectionFactory _factory;

        public SalesService(ICreateSaleHandler createSaleHandler, ISqlConnectionFactory factory)
        {
            _createSaleHandler = createSaleHandler;
            _factory = factory;
        }

        public CreateSaleResponse CreateSale(CreateSaleRequest request, int userId)
            => _createSaleHandler.Handle(request, userId);

        public List<SalesInvoiceLookupDto> SearchInvoices(string? query)
        {
            using var session = new DbSession(_factory);
            var salesRepository = new SalesRepository(session);
            return salesRepository.SearchInvoices(query);
        }

        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId)
        {
            using var session = new DbSession(_factory);
            var salesRepository = new SalesRepository(session);
            return salesRepository.GetInvoiceById(invoiceId);
        }

        public bool UpdateInvoice(int invoiceId, UpdateSaleRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var salesRepository = new SalesRepository(session);
            var updated = salesRepository.UpdateInvoice(invoiceId, request, userId);
            session.Commit();
            return updated;
        }
    }
}
