using Microsoft.Azure.Documents;

namespace OpenCqrs.Store.Cosmos.Sql.Configuration
{
    public class CosmosDbOptions
    {
        public string DatabaseId { get; set; } = "DomainStore";
        public string AggregateCollectionId { get; set; } = "Aggregates";
        public string CommandCollectionId { get; set; } = "Commands";
        public string EventCollectionId { get; set; } = "Events";
        public int OfferThroughput { get; set; } = 400;
        public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;
    }
}
