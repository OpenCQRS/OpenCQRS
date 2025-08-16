using System;

namespace OpenCqrs.Store.Cosmos.Mongo.Documents.Factories
{
    public class AggregateDocumentFactory : IAggregateDocumentFactory
    {
        public AggregateDocument CreateAggregate(Type aggregateType, Guid aggregateRootId)
        {
            return new AggregateDocument
            {
                Id = aggregateRootId.ToString(),
                Type = aggregateType.AssemblyQualifiedName
            };
        }
    }
}