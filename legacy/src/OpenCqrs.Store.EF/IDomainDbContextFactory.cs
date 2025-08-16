namespace OpenCqrs.Store.EF
{
    public interface IDomainDbContextFactory
    {
        DomainDbContext CreateDbContext();
    }
}
