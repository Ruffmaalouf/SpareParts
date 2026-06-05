using SpareParts.Domain.BusinessPartners;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ISuppliersRepository
    {
        IEnumerable<SupplierDto> GetAll();
        (IEnumerable<SupplierDto> Items, int TotalCount) GetPaged(int page, int pageSize);
        int Insert(Supplier supplier);
        Supplier? GetById(int id);
        bool Update(int id, CreateSupplierRequest request, int userId);
        void SetAccountId(int id, int? accountId, int userId);
        int? GetAccountId(int id);
        bool UsesAccount(int accountId);
        bool Delete(int id);
    }
}
