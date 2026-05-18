using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface ICreateSaleHandler
    {
        CreateSaleResponse Handle(CreateSaleRequest request, int userId);
    }
}
