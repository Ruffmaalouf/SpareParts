using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IPartsRepository
    {
        Dictionary<int, Part> GetByIds(IList<int> partIds);
        IEnumerable<Part> GetAllActive();
        int Insert(Part part);
        bool Update(int id, CreatePartRequest request, int userId);
        bool Delete(int id);
    }
}
