using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ITransactionTypesRepository
    {
        IEnumerable<TransactionTypeDto> GetAll();
        TransactionTypeDto? GetById(int id);
        void Insert(string typeKey, string name, string currencyCode, decimal counterRate, string serialNumberFormat, long startNumber, long currentNumber, bool isActive);
        bool Update(int id, string typeKey, string name, string currencyCode, decimal counterRate, string serialNumberFormat, long startNumber, long currentNumber, bool isActive);
        bool Delete(int id);
    }
}
