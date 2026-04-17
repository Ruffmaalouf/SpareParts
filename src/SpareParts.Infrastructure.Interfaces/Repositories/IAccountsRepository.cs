using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IAccountsRepository
    {
        IEnumerable<AccountDto> GetAll();
        Account? GetById(int id);
        Account? GetByCode(string code);
        int Insert(Account account);
        bool Update(int id, string code, string name, string accountTypeKey, int? parentId, int userId);
        bool Delete(int id);
        bool HasChildren(int id);
    }
}
