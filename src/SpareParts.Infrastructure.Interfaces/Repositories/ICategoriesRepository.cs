using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ICategoriesRepository
    {
        IEnumerable<Category> GetAll();
        int Insert(Category category);
    }
}
