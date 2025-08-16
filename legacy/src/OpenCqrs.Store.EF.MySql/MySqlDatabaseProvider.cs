using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenCqrs.Store.EF.Configuration;

namespace OpenCqrs.Store.EF.MySql
{
    public class MySqlDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;

        public MySqlDatabaseProvider(IOptions<DatabaseOptions> settings)
        {
            _connectionString = settings.Value.ConnectionString;
        }

        public DomainDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DomainDbContext>();
            optionsBuilder.UseMySQL(_connectionString);

            return new DomainDbContext(optionsBuilder.Options);
        }
    }
}