using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the domain service for managing aggregates and domain events.
/// </summary>
public class EntityFrameworkCoreDomainService(IDomainDbContext domainDbContext) : IDomainService
{
    /// <summary>
    /// Gets an aggregate from the specified stream with optional application of new domain events.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to retrieve.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="applyNewDomainEvents">Whether to apply new domain events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the aggregate.</returns>
    public async Task<Result<TAggregate>> GetAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetAggregate(streamId, aggregateId, applyNewDomainEvents, cancellationToken);
    }

    /// <summary>
    /// Gets domain events from the specified stream with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="eventTypeFilter">Optional array of event types to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events.</returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEvents(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEvents(streamId, eventTypeFilter, cancellationToken);
    }

    /// <summary>
    /// Gets domain events that have been applied to a specific aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events applied to the aggregate.</returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEventsAppliedToAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetDomainEventsAppliedToAggregate(aggregateId, cancellationToken);
    }

    /// <summary>
    /// Gets domain events between two specific sequence numbers with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="fromSequence">The starting sequence number (inclusive).</param>
    /// <param name="toSequence">The ending sequence number (inclusive).</param>
    /// <param name="eventTypeFilter">An optional array of event types to filter the retrieved domain events.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEventsBetweenSequences(IStreamId streamId, int fromSequence, int toSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsBetweenSequences(streamId, fromSequence, toSequence, eventTypeFilter, cancellationToken);
    }

    /// <summary>
    /// Gets domain events from a specific sequence number onwards with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="fromSequence">The sequence number to start from.</param>
    /// <param name="eventTypeFilter">Optional array of event types to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events from the specified sequence.</returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEventsFromSequence(IStreamId streamId, int fromSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsFromSequence(streamId, fromSequence, eventTypeFilter, cancellationToken);
    }

    /// <summary>
    /// Gets domain events up to a specific sequence number with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="upToSequence">The sequence number to read up to.</param>
    /// <param name="eventTypeFilter">Optional array of event types to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events up to the specified sequence.</returns>
    public async Task<Result<List<IDomainEvent>>> GetDomainEventsUpToSequence(IStreamId streamId, int upToSequence, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsUpToSequence(streamId, upToSequence, eventTypeFilter, cancellationToken);
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsUpToDate(IStreamId streamId, DateTimeOffset upToDate, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsUpToDate(streamId, upToDate, eventTypeFilter, cancellationToken);
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsFromDate(IStreamId streamId, DateTimeOffset fromDate, Type[]? eventTypeFilter = null,
        CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsFromDate(streamId, fromDate, eventTypeFilter, cancellationToken);
    }

    public async Task<Result<List<IDomainEvent>>> GetDomainEventsBetweenDates(IStreamId streamId, DateTimeOffset fromDate, DateTimeOffset toDate,
        Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetDomainEventsBetweenDates(streamId, fromDate, toDate, eventTypeFilter, cancellationToken);
    }

    /// <summary>
    /// Gets an in-memory aggregate optionally up to a specific sequence number.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to retrieve.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="upToSequence">Optional sequence number to read up to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the in-memory aggregate.</returns>
    public async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, int? upToSequence = null, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetInMemoryAggregate(streamId, aggregateId, upToSequence, cancellationToken);
    }

    public async Task<Result<TAggregate>> GetInMemoryAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, DateTimeOffset upToDate,
        CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.GetInMemoryAggregate(streamId, aggregateId, upToDate, cancellationToken);
    }

    /// <summary>
    /// Gets the latest event sequence number from the specified stream with optional event type filtering.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="eventTypeFilter">Optional array of event types to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the latest event sequence number.</returns>
    public async Task<Result<int>> GetLatestEventSequence(IStreamId streamId, Type[]? eventTypeFilter = null, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.GetLatestEventSequence(streamId, eventTypeFilter, cancellationToken);
    }

    /// <summary>
    /// Saves an aggregate to the specified stream with expected event sequence validation.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to save.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="expectedEventSequence">The expected event sequence for optimistic concurrency control.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating the success or failure of the operation.</returns>
    public async Task<Result> SaveAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.SaveAggregate(streamId, aggregateId, aggregate, expectedEventSequence, cancellationToken);
    }

    /// <summary>
    /// Saves domain events to the specified stream with expected event sequence validation.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="domainEvents">The domain events to save.</param>
    /// <param name="expectedEventSequence">The expected event sequence for optimistic concurrency control.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating the success or failure of the operation.</returns>
    public async Task<Result> SaveDomainEvents(IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        return await domainDbContext.SaveDomainEvents(streamId, domainEvents, expectedEventSequence, cancellationToken);
    }

    /// <summary>
    /// Updates an aggregate from the specified stream.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate to update.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the updated aggregate.</returns>
    public async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        return await domainDbContext.UpdateAggregate(streamId, aggregateId, cancellationToken);
    }

    /// <summary>
    /// Disposes the domain service and its underlying database context.
    /// </summary>
    public void Dispose() => domainDbContext.Dispose();
}
