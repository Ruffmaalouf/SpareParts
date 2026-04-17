using SpareParts.Domain.BusinessPartners;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ICustomersRepository
    {
        IEnumerable<CustomerDto> GetAll();
        int Insert(Customer customer);
        Customer? GetById(int id);
        bool Update(int id, CreateCustomerRequest request, int userId);
        void SetAccountId(int id, int? accountId, int userId);
        int? GetAccountId(int id);
        bool UsesAccount(int accountId);
        bool Delete(int id);
    }
}
