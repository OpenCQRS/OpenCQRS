using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenCqrs.Domain;
using OpenCqrs.Store.Cosmos.Sql.Documents;
using OpenCqrs.Store.Cosmos.Sql.Documents.Factories;
using OpenCqrs.Store.Cosmos.Sql.Repositories;

namespace OpenCqrs.Store.Cosmos.Sql
{
    public class CosmosStoreProvider : IStoreProvider
    {
        private readonly IDocumentRepository<AggregateDocument> _aggregateRepository;
        private readonly IDocumentRepository<CommandDocument> _commandRepository;
        private readonly IDocumentRepository<EventDocument> _eventRepository;
        private readonly IAggregateDocumentFactory _aggregateDocumentFactory;
        private readonly ICommandDocumentFactory _commandDocumentFactory;
        private readonly IEventDocumentFactory _eventDocumentFactory;
        private readonly IVersionService _versionService;

        public CosmosStoreProvider(IDocumentRepository<AggregateDocument> aggregateRepository,
            IDocumentRepository<CommandDocument> commandRepository,
            IDocumentRepository<EventDocument> eventRepository,
            IAggregateDocumentFactory aggregateDocumentFactory,
            ICommandDocumentFactory commandDocumentFactory,
            IEventDocumentFactory eventDocumentFactory,
            IVersionService versionService)
        {
            _aggregateRepository = aggregateRepository;
            _commandRepository = commandRepository;
            _eventRepository = eventRepository;
            _aggregateDocumentFactory = aggregateDocumentFactory;
            _commandDocumentFactory = commandDocumentFactory;
            _commandDocumentFactory = commandDocumentFactory;
            _eventDocumentFactory = eventDocumentFactory;
            _versionService = versionService;
        }

        public IEnumerable<IDomainEvent> GetEvents(Guid aggregateId)
        {
            var result = new List<DomainEvent>();

            var events = _eventRepository.GetDocumentsAsync(d => d.AggregateId == aggregateId).GetAwaiter().GetResult();

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

            var events = await _eventRepository.GetDocumentsAsync(d => d.AggregateId == aggregateId);

            foreach (var @event in events)
            {
                var domainEvent = JsonConvert.DeserializeObject(@event.Data, Type.GetType(@event.Type));
                result.Add((DomainEvent)domainEvent);
            }

            return result;
        }

        public void Save(SaveStoreData request)
        {
            var aggregateDocument = _aggregateRepository.GetDocumentAsync(request.AggregateRootId.ToString(), request.AggregateType.AssemblyQualifiedName).GetAwaiter().GetResult();
            if (aggregateDocument == null)
            {
                var newAggregateDocument = _aggregateDocumentFactory.CreateAggregate(request.AggregateType, request.AggregateRootId);
                _aggregateRepository.CreateDocumentAsync(newAggregateDocument, request.AggregateType.AssemblyQualifiedName).GetAwaiter().GetResult();
            }

            if (request.DomainCommand != null)
            {
                var commandDocument = _commandDocumentFactory.CreateCommand(request.DomainCommand);
                _commandRepository.CreateDocumentAsync(commandDocument, request.DomainCommand.GetType().AssemblyQualifiedName).GetAwaiter().GetResult();
            }

            foreach (var @event in request.Events)
            {
                var currentVersion = _eventRepository.GetCountAsync(d => d.AggregateId == @event.AggregateRootId).GetAwaiter().GetResult();
                var nextVersion = _versionService.GetNextVersion(@event.AggregateRootId, currentVersion, request.DomainCommand?.ExpectedVersion);

                var eventDocument = _eventDocumentFactory.CreateEvent(@event, nextVersion);

                _eventRepository.CreateDocumentAsync(eventDocument, @event.GetType().AssemblyQualifiedName).GetAwaiter().GetResult();
            }
        }

        public async Task SaveAsync(SaveStoreData request)
        {
            var aggregateDocument = await _aggregateRepository.GetDocumentAsync(request.AggregateRootId.ToString(), request.AggregateType.AssemblyQualifiedName);
            if (aggregateDocument == null)
            {
                var newAggregateDocument = _aggregateDocumentFactory.CreateAggregate(request.AggregateType, request.AggregateRootId);
                await _aggregateRepository.CreateDocumentAsync(newAggregateDocument, request.AggregateType.AssemblyQualifiedName);
            }

            if (request.DomainCommand != null)
            {
                var commandDocument = _commandDocumentFactory.CreateCommand(request.DomainCommand);
                await _commandRepository.CreateDocumentAsync(commandDocument, request.DomainCommand.GetType().AssemblyQualifiedName);
            }

            foreach (var @event in request.Events)
            {
                var currentVersion = await _eventRepository.GetCountAsync(d => d.AggregateId == @event.AggregateRootId);
                var nextVersion = _versionService.GetNextVersion(@event.AggregateRootId, currentVersion, request.DomainCommand?.ExpectedVersion);

                var eventDocument = _eventDocumentFactory.CreateEvent(@event, nextVersion);

                await _eventRepository.CreateDocumentAsync(eventDocument, @event.GetType().AssemblyQualifiedName);
            }
        }
    }
}
