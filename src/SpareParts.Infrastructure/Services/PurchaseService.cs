using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Services
{
    public interface ICreatePurchaseHandler
    {
        CreatePurchaseResponse Handle(CreatePurchaseRequest request, int userId);
    }

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
