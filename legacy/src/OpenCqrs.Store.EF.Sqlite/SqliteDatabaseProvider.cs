using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenCqrs.Store.EF.Configuration;

namespace OpenCqrs.Store.EF.Sqlite
{
    public class SqliteDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;

        public SqliteDatabaseProvider(IOptions<DatabaseOptions> settings)
        {
            _connectionString = settings.Value.ConnectionString;
        }

        public DomainDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DomainDbContext>();
            optionsBuilder.UseSqlite(_connectionString);

            return new DomainDbContext(optionsBuilder.Options);
        }
    }
}