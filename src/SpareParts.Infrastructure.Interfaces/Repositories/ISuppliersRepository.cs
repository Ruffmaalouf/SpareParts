using SpareParts.Domain.BusinessPartners;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ISuppliersRepository
    {
        IEnumerable<Supplier> GetAll();
        int Insert(Supplier supplier);
        bool Update(int id, CreateSupplierRequest request, int userId);
        bool Delete(int id);
    }
}
