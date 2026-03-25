using SpareParts.Domain.BusinessPartners;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ICustomersRepository
    {
        IEnumerable<Customer> GetAll();
        int Insert(Customer customer);
        bool Update(int id, CreateCustomerRequest request, int userId);
        bool Delete(int id);
    }
}
