using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IBrandsRepository
    {
        IEnumerable<Brand> GetAll();
        int Insert(Brand brand);
        bool Update(int id, string name, bool isActive, int userId);
        bool Delete(int id);
    }
}
