using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseService
    {
        private readonly ICreatePurchaseHandler _createPurchaseHandler;

        public PurchaseService(ICreatePurchaseHandler createPurchaseHandler)
        {
            _createPurchaseHandler = createPurchaseHandler;
        }

        public CreatePurchaseResponse CreatePurchase(CreatePurchaseRequest request, int userId)
            => _createPurchaseHandler.Handle(request, userId);
    }
}
