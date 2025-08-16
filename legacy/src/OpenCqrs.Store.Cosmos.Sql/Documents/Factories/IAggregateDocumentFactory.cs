using System;

namespace OpenCqrs.Store.Cosmos.Sql.Documents.Factories
{
    public interface IAggregateDocumentFactory
    {
        AggregateDocument CreateAggregate(Type aggregateType, Guid aggregateRootId);
    }
}
