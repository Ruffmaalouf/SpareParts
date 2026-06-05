using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IBrandsRepository
    {
        IEnumerable<Brand> GetAll();
        (IEnumerable<Brand> Items, int TotalCount) GetPaged(int page, int pageSize);
        int Insert(Brand brand);
        bool Update(int id, string name, bool isActive, int userId);
        bool Delete(int id);
    }
}
