using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseService
    {
        private readonly ICreatePurchaseHandler _createPurchaseHandler;
        private readonly ICreateUsedCarPurchaseHandler _createUsedCarPurchaseHandler;
        private readonly ISqlConnectionFactory _factory;

        public PurchaseService(
            ICreatePurchaseHandler createPurchaseHandler,
            ICreateUsedCarPurchaseHandler createUsedCarPurchaseHandler,
            ISqlConnectionFactory factory)
        {
            _createPurchaseHandler = createPurchaseHandler;
            _createUsedCarPurchaseHandler = createUsedCarPurchaseHandler;
            _factory = factory;
        }

        public CreatePurchaseResponse CreatePurchase(CreatePurchaseRequest request, int userId)
            => _createPurchaseHandler.Handle(request, userId);

        public CreateUsedCarPurchaseResponse CreateUsedCarPurchase(CreateUsedCarPurchaseRequest request, int userId)
            => _createUsedCarPurchaseHandler.Handle(request, userId);

        public IReadOnlyList<UsedCarPurchaseSummaryDto> GetUsedCarPurchases()
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Purchases.UsedCarPurchases.GetAll();
        }
    }
}
