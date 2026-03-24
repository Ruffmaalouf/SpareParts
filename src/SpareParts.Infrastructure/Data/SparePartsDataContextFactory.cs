namespace SpareParts.Infrastructure.Data
{
    public interface ISparePartsDataContextFactory
    {
        SparePartsDataContext Create(DbSession session);
    }

    public class SparePartsDataContextFactory : ISparePartsDataContextFactory
    {
        public SparePartsDataContext Create(DbSession session)
        {
            return new SparePartsDataContext(session);
        }
    }
}
