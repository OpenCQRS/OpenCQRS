using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenCqrs.Extensions;
using OpenCqrs.Store.EF.Configuration;
using OpenCqrs.Store.EF.Extensions;

namespace OpenCqrs.Store.EF.Sqlite.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddSqliteStore(this IOpenCqrsServiceBuilder builder)
        {
            return AddSqliteStore(builder, opt => { });
        }

        public static IOpenCqrsServiceBuilder AddSqliteStore(this IOpenCqrsServiceBuilder builder, Action<DatabaseOptions> configureOptions)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            builder.Services.Configure(configureOptions);

            var sp = builder.Services.BuildServiceProvider();
            var dbOptions = sp.GetService<IOptions<DatabaseOptions>>().Value;

            builder.AddEFStore();

            builder.Services.AddDbContext<DomainDbContext>(options =>
                options.UseSqlite(dbOptions.ConnectionString));

            builder.Services.AddTransient<IDatabaseProvider, SqliteDatabaseProvider>();

            return builder;
        }
    }
}
