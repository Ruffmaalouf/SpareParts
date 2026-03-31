using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ITransactionTypesRepository
    {
        IEnumerable<TransactionTypeDto> GetAll();
        void Insert(string name, string currencyCode, decimal counterRate, bool isActive);
        bool Update(int id, string name, string currencyCode, decimal counterRate, bool isActive);
        bool Delete(int id);
    }
}
