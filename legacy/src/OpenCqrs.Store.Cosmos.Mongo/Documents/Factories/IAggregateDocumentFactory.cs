using System;

namespace OpenCqrs.Store.Cosmos.Mongo.Documents.Factories
{
    public interface IAggregateDocumentFactory
    {
        AggregateDocument CreateAggregate(Type aggregateType, Guid aggregateRootId);
    }
}
