using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class SalesService
    {
        private readonly ICreateSaleHandler _createSaleHandler;

        public SalesService(ICreateSaleHandler createSaleHandler)
        {
            _createSaleHandler = createSaleHandler;
        }

        public CreateSaleResponse CreateSale(CreateSaleRequest request, int userId)
            => _createSaleHandler.Handle(request, userId);
    }
}
