using MongoDB.Bson.Serialization.Attributes;

namespace OpenCqrs.Store.Cosmos.Mongo.Documents
{
    public class AggregateDocument
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }
    }
}
