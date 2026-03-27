namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class MasterDataRepositories
    {
        internal MasterDataRepositories(DbSession session) => Parts = new PartsRepository(session);
        public IPartsRepository Parts { get; }
    }
}
