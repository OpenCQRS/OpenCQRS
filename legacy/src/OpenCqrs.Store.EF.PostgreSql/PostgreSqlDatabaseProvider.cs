using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenCqrs.Store.EF.Configuration;

namespace OpenCqrs.Store.EF.PostgreSql
{
    public class PostgreSqlDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;

        public PostgreSqlDatabaseProvider(IOptions<DatabaseOptions> settings)
        {
            _connectionString = settings.Value.ConnectionString;
        }

        public DomainDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DomainDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);

            return new DomainDbContext(optionsBuilder.Options);
        }
    }
}