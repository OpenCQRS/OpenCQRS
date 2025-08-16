using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenCqrs.Store.Cosmos.Mongo.Configuration;
using OpenCqrs.Store.Cosmos.Mongo.Documents;

namespace OpenCqrs.Store.Cosmos.Mongo
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly string _aggregateCollectionName;
        private readonly string _commandCollectionName;
        private readonly string _eventCollectionName;

        public MongoDbContext(IOptions<MongoOptions> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
            _aggregateCollectionName = settings.Value.AggregateCollectionName;
            _commandCollectionName = settings.Value.CommandCollectionName;
            _eventCollectionName = settings.Value.EventCollectionName;
        }

        public IMongoCollection<AggregateDocument> Aggregates => 
            _database.GetCollection<AggregateDocument>(_aggregateCollectionName);

        public IMongoCollection<CommandDocument> Commands =>
            _database.GetCollection<CommandDocument>(_commandCollectionName);

        public IMongoCollection<EventDocument> Events =>
            _database.GetCollection<EventDocument>(_eventCollectionName);
    }
}