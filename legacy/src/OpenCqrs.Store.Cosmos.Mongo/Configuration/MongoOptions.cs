namespace OpenCqrs.Store.Cosmos.Mongo.Configuration
{
    public class MongoOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; } = "DomainStore";
        public string AggregateCollectionName { get; set; } = "Aggregates";
        public string CommandCollectionName { get; set; } = "Commands";
        public string EventCollectionName { get; set; } = "Events";
    }
}
