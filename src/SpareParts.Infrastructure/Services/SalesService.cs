using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Services
{
    public interface ICreateSaleHandler
    {
        CreateSaleResponse Handle(CreateSaleRequest request, int userId);
    }

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
