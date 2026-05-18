using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface ICreatePurchaseHandler
    {
        CreatePurchaseResponse Handle(CreatePurchaseRequest request, int userId);
    }
}
