namespace OpenCqrs.Store.EF
{
    public interface IDatabaseProvider
    {
        DomainDbContext CreateDbContext();
    }
}
