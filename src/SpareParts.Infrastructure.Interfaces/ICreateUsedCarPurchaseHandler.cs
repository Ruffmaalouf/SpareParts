using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface ICreateUsedCarPurchaseHandler
    {
        CreateUsedCarPurchaseResponse Handle(CreateUsedCarPurchaseRequest request, int userId);
    }
}
