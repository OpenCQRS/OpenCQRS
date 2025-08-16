using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using OpenCqrs.Domain;
using OpenCqrs.Store.Cosmos.Mongo.Configuration;
using OpenCqrs.Store.Cosmos.Mongo.Documents;
using OpenCqrs.Store.Cosmos.Mongo.Documents.Factories;

namespace OpenCqrs.Store.Cosmos.Mongo
{
    public class MongoStoreProvider : IStoreProvider
    {
        private readonly MongoDbContext _dbContext;
        private readonly IAggregateDocumentFactory _aggregateDocumentFactory;
        private readonly ICommandDocumentFactory _commandDocumentFactory;
        private readonly IEventDocumentFactory _eventDocumentFactory;
        private readonly IVersionService _versionService;

        public MongoStoreProvider(IOptions<MongoOptions> settings,
            IAggregateDocumentFactory aggregateDocumentFactory,
            ICommandDocumentFactory commandDocumentFactory,
            IEventDocumentFactory eventDocumentFactory,
            IVersionService versionService)
        {
            _dbContext = new MongoDbContext(settings);
            _aggregateDocumentFactory = aggregateDocumentFactory;
            _commandDocumentFactory = commandDocumentFactory;
            _commandDocumentFactory = commandDocumentFactory;
            _eventDocumentFactory = eventDocumentFactory;
            _versionService = versionService;
        }

        public IEnumerable<IDomainEvent> GetEvents(Guid aggregateId)
        {
            var result = new List<DomainEvent>();

            var filter = Builders<EventDocument>.Filter.Eq("aggregateId", aggregateId.ToString());
            var events = _dbContext.Events.Find(filter).ToList();

            foreach (var @event in events)
            {
                var domainEvent = JsonConvert.DeserializeObject(@event.Data, Type.GetType(@event.Type));
                result.Add((DomainEvent)domainEvent);
            }

            return result;
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId)
        {
            var result = new List<DomainEvent>();

            var filter = Builders<EventDocument>.Filter.Eq("aggregateId", aggregateId.ToString());
            var events = await _dbContext.Events.Find(filter).ToListAsync();

            foreach (var @event in events)
            {
                var domainEvent = JsonConvert.DeserializeObject(@event.Data, Type.GetType(@event.Type));
                result.Add((DomainEvent)domainEvent);
            }

            return result;
        }

        public void Save(SaveStoreData request)
        {
            var aggregateFilter = Builders<AggregateDocument>.Filter.Eq("_id", request.AggregateRootId.ToString());
            var aggregateDocument = _dbContext.Aggregates.Find(aggregateFilter).FirstOrDefault();
            if (aggregateDocument == null)
            {
                var newAggregateDocument = _aggregateDocumentFactory.CreateAggregate(request.AggregateType, request.AggregateRootId);
                _dbContext.Aggregates.InsertOne(newAggregateDocument);
            }

            if (request.DomainCommand != null)
            {
                var commandDocument = _commandDocumentFactory.CreateCommand(request.DomainCommand);
                _dbContext.Commands.InsertOne(commandDocument);
            }

            foreach (var @event in request.Events)
            {
                var eventFilter = Builders<EventDocument>.Filter.Eq("aggregateId", @event.AggregateRootId.ToString());
                var currentVersion = _dbContext.Events.Find(eventFilter).CountDocuments();
                var nextVersion = _versionService.GetNextVersion(@event.AggregateRootId, (int)currentVersion, request.DomainCommand?.ExpectedVersion);

                var eventDocument = _eventDocumentFactory.CreateEvent(@event, nextVersion);

                _dbContext.Events.InsertOne(eventDocument);
            }
        }

        public async Task SaveAsync(SaveStoreData request)
        {
            var aggregateFilter = Builders<AggregateDocument>.Filter.Eq("_id", request.AggregateRootId.ToString());
            var aggregateDocument = await _dbContext.Aggregates.Find(aggregateFilter).FirstOrDefaultAsync();
            if (aggregateDocument == null)
            {
                var newAggregateDocument = _aggregateDocumentFactory.CreateAggregate(request.AggregateType, request.AggregateRootId);
                await _dbContext.Aggregates.InsertOneAsync(newAggregateDocument);
            }

            if (request.DomainCommand != null)
            {
                var commandDocument = _commandDocumentFactory.CreateCommand(request.DomainCommand);
                await _dbContext.Commands.InsertOneAsync(commandDocument);
            }

            foreach (var @event in request.Events)
            {
                var eventFilter = Builders<EventDocument>.Filter.Eq("aggregateId", @event.AggregateRootId.ToString());
                var currentVersion = await _dbContext.Events.Find(eventFilter).CountDocumentsAsync();
                var nextVersion = _versionService.GetNextVersion(@event.AggregateRootId, (int)currentVersion, request.DomainCommand?.ExpectedVersion);

                var eventDocument = _eventDocumentFactory.CreateEvent(@event, nextVersion);

                await _dbContext.Events.InsertOneAsync(eventDocument);
            }
        }
    }
}
