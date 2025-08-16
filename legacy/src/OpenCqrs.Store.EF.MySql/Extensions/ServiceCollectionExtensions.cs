using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenCqrs.Extensions;
using OpenCqrs.Store.EF.Configuration;
using OpenCqrs.Store.EF.Extensions;

namespace OpenCqrs.Store.EF.MySql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenCqrsServiceBuilder AddMySqlStore(this IOpenCqrsServiceBuilder builder)
        {
            return AddMySqlStore(builder, opt => { });
        }

        public static IOpenCqrsServiceBuilder AddMySqlStore(this IOpenCqrsServiceBuilder builder, Action<DatabaseOptions> configureOptions)
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
                options.UseMySQL(dbOptions.ConnectionString));

            builder.Services.AddTransient<IDatabaseProvider, MySqlDatabaseProvider>();

            return builder;
        }
    }
}
