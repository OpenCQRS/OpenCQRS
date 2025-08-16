using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenCqrs.Domain;
using OpenCqrs.Store.EF.Entities.Factories;

namespace OpenCqrs.Store.EF
{
    public class StoreProvider : IStoreProvider
    {
        private readonly IDomainDbContextFactory _dbContextFactory;
        private readonly IAggregateEntityFactory _aggregateEntityFactory;
        private readonly ICommandEntityFactory _commandEntityFactory;
        private readonly IEventEntityFactory _eventEntityFactory;
        private readonly IVersionService _versionService;

        public StoreProvider(IDomainDbContextFactory dbContextFactory, 
            IAggregateEntityFactory aggregateEntityFactory,
            ICommandEntityFactory commandEntityFactory,
            IEventEntityFactory eventEntityFactory,
            IVersionService versionService)
        {
            _dbContextFactory = dbContextFactory;
            _aggregateEntityFactory = aggregateEntityFactory;
            _commandEntityFactory = commandEntityFactory;
            _eventEntityFactory = eventEntityFactory;
            _versionService = versionService;
        }

        public IEnumerable<IDomainEvent> GetEvents(Guid aggregateId)
        {
            var result = new List<DomainEvent>();

            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var events = dbContext.Events
                    .Where(x => x.AggregateId == aggregateId)
                    .OrderBy(x => x.Sequence)
                    .ToList();

                foreach (var @event in events)
                {
                    var domainEvent = JsonConvert.DeserializeObject(@event.Data, Type.GetType(@event.Type));
                    result.Add((DomainEvent)domainEvent);
                }
            }

            return result;
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId)
        {
            var result = new List<DomainEvent>();

            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var events = await dbContext.Events
                    .Where(x => x.AggregateId == aggregateId)
                    .OrderBy(x => x.Sequence)
                    .ToListAsync();

                foreach (var @event in events)
                {
                    var domainEvent = JsonConvert.DeserializeObject(@event.Data, Type.GetType(@event.Type));
                    result.Add((DomainEvent)domainEvent);
                }
            }

            return result;
        }

        public void Save(SaveStoreData request)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var aggregateEntity = dbContext.Aggregates.FirstOrDefault(x => x.Id == request.AggregateRootId);
                if (aggregateEntity == null)
                {
                    var newAggregateEntity = _aggregateEntityFactory.CreateAggregate(request.AggregateType, request.AggregateRootId);
                    dbContext.Aggregates.Add(newAggregateEntity);
                }

                if (request.DomainCommand != null)
                {
                    var newCommandEntity = _commandEntityFactory.CreateCommand(request.DomainCommand);
                    dbContext.Commands.Add(newCommandEntity);
                }

                foreach (var @event in request.Events)
                {
                    var currentVersion = dbContext.Events.Count(x => x.AggregateId == @event.AggregateRootId);
                    var nextVersion = _versionService.GetNextVersion(@event.AggregateRootId, currentVersion, request.DomainCommand?.ExpectedVersion);
                    var newEventEntity = _eventEntityFactory.CreateEvent(@event, nextVersion);
                    dbContext.Events.Add(newEventEntity);
                }

                dbContext.SaveChanges();
            }
        }

        public async Task SaveAsync(SaveStoreData request)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var aggregateEntity = await dbContext.Aggregates.FirstOrDefaultAsync(x => x.Id == request.AggregateRootId);
                if (aggregateEntity == null)
                {
                    var newAggregateEntity = _aggregateEntityFactory.CreateAggregate(request.AggregateType, request.AggregateRootId);
                    await dbContext.Aggregates.AddAsync(newAggregateEntity);
                }

                if (request.DomainCommand != null)
                {
                    var newCommandEntity = _commandEntityFactory.CreateCommand(request.DomainCommand);
                    await dbContext.Commands.AddAsync(newCommandEntity);
                }

                foreach (var @event in request.Events)
                {
                    var currentVersion = await dbContext.Events.CountAsync(x => x.AggregateId == @event.AggregateRootId);
                    var nextVersion = _versionService.GetNextVersion(@event.AggregateRootId, currentVersion, request.DomainCommand?.ExpectedVersion);
                    var newEventEntity = _eventEntityFactory.CreateEvent(@event, nextVersion);
                    await dbContext.Events.AddAsync(newEventEntity);
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
