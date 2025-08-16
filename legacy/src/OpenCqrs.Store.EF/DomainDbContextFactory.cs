using System;
using OpenCqrs.Dependencies;

namespace OpenCqrs.Store.EF
{
    public class DomainDbContextFactory : IDomainDbContextFactory
    {
        private readonly IResolver _resolver;

        public DomainDbContextFactory(IResolver resolver)
        {
            _resolver = resolver;
        }

        public DomainDbContext CreateDbContext()
        {
            var dataProvider = _resolver.Resolve<IDatabaseProvider>();

            if (dataProvider == null)
                throw new ApplicationException("Domain database provider not found.");

            return dataProvider.CreateDbContext();
        }
    }
}
