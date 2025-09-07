using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Retrieves an aggregate from the event store, either from its snapshot or by reconstructing it from events.
    /// This method provides flexible aggregate loading with options for applying new events and automatic
    /// snapshot management for optimal performance in event sourcing scenarios.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate to retrieve. Must implement <see cref="IAggregate"/> and have a parameterless
    /// constructor to support both snapshot deserialization and event-based reconstruction.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to aggregate snapshots, events, and related
    /// entities for aggregate retrieval and reconstruction operations.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream containing the aggregate's events. Used for event
    /// retrieval when snapshot reconstruction or event application is required.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the specific aggregate instance to retrieve.
    /// Used for snapshot lookup and aggregate identification operations.
    /// </param>
    /// <param name="applyNewDomainEvents">
    /// When true, applies any new events that occurred after the snapshot was created, ensuring
    /// the returned aggregate reflects the most current state. When false, returns the aggregate
    /// as it existed at the time of the last snapshot.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result{TAggregate}"/> containing either the successfully retrieved and potentially
    /// updated aggregate, or a <see cref="Failure"/> if the aggregate type lacks required metadata
    /// or other retrieval issues occur.
    /// </returns>
    /// <example>
    /// <code>
    /// // Basic aggregate retrieval from snapshot
    /// public async Task&lt;OrderAggregate?&gt; GetOrderAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     var result = await _context.GetAggregate(streamId, aggregateId);
    ///     return result.IsSuccess ? result.Value : null;
    /// }
    /// 
    /// // Retrieve aggregate with latest events applied
    /// public async Task&lt;Result&lt;OrderAggregate&gt;&gt; GetCurrentOrderStateAsync(Guid orderId)
    /// {
    ///     var streamId = new OrderStreamId(orderId);
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     // Ensure aggregate includes any new events since last snapshot
    ///     return await _context.GetAggregate(streamId, aggregateId, applyNewDomainEvents: true);
    /// }
    /// 
    /// // Repository pattern implementation
    /// public class OrderRepository
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public OrderRepository(IDomainDbContext context)
    ///     {
    ///         _context = context;
    ///     }
    ///     
    ///     public async Task&lt;OrderAggregate?&gt; FindByIdAsync(Guid orderId)
    ///     {
    ///         var streamId = new OrderStreamId(orderId);
    ///         var aggregateId = new OrderAggregateId(orderId);
    ///         
    ///         var result = await _context.GetAggregate(streamId, aggregateId, applyNewDomainEvents: true);
    ///         
    ///         if (result.IsNotSuccess)
    ///         {
    ///             _logger.LogWarning("Failed to retrieve order {OrderId}: {Error}", 
    ///                 orderId, result.Failure?.Description);
    ///             return null;
    ///         }
    ///         
    ///         return result.Value?.Version &gt; 0 ? result.Value : null; // Return null for non-existent aggregates
    ///     }
    /// }
    /// 
    /// // Command handler with aggregate retrieval
    /// public class UpdateOrderCommandHandler : IRequestHandler&lt;UpdateOrderCommand&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; Handle(UpdateOrderCommand command, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new OrderStreamId(command.OrderId);
    ///         var aggregateId = new OrderAggregateId(command.OrderId);
    ///         
    ///         // Get current aggregate state for modification
    ///         var aggregateResult = await _context.GetAggregate(streamId, aggregateId, 
    ///             applyNewDomainEvents: true, cancellationToken);
    ///             
    ///         if (aggregateResult.IsNotSuccess)
    ///             return aggregateResult.Failure!;
    ///         
    ///         var order = aggregateResult.Value!;
    ///         if (order.Version == 0)
    ///             return new Failure("Order not found", $"Order {command.OrderId} does not exist");
    ///         
    ///         // Apply business logic
    ///         order.UpdateShippingAddress(command.NewAddress);
    ///         
    ///         // Save changes
    ///         var expectedSequence = await _context.GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
    ///         return await _context.Save(streamId, aggregateId, order, expectedSequence, cancellationToken);
    ///     }
    /// }
    /// 
    /// // Bulk aggregate loading for reporting
    /// public async Task&lt;List&lt;OrderAggregate&gt;&gt; GetOrdersForCustomerAsync(Guid customerId)
    /// {
    ///     var orderIds = await GetOrderIdsByCustomerAsync(customerId);
    ///     var orders = new List&lt;OrderAggregate&gt;();
    ///     
    ///     foreach (var orderId in orderIds)
    ///     {
    ///         var streamId = new OrderStreamId(orderId);
    ///         var aggregateId = new OrderAggregateId(orderId);
    ///         
    ///         var result = await _context.GetAggregate(streamId, aggregateId, applyNewDomainEvents: false);
    ///         if (result.IsSuccess && result.Value!.Version &gt; 0)
    ///         {
    ///             orders.Add(result.Value);
    ///         }
    ///     }
    ///     
    ///     return orders;
    /// }
    /// 
    /// // Performance comparison between snapshot and fresh reconstruction
    /// public async Task&lt;AggregatePerformanceMetrics&gt; CompareLoadMethodsAsync(Guid aggregateId)
    /// {
    ///     var streamId = new OrderStreamId(aggregateId);
    ///     var aggregateIdObj = new OrderAggregateId(aggregateId);
    ///     
    ///     var metrics = new AggregatePerformanceMetrics();
    ///     
    ///     // Time snapshot-based loading
    ///     var stopwatch = Stopwatch.StartNew();
    ///     var snapshotResult = await _context.GetAggregate(streamId, aggregateIdObj, applyNewDomainEvents: false);
    ///     stopwatch.Stop();
    ///     metrics.SnapshotLoadTime = stopwatch.Elapsed;
    ///     
    ///     // Time full reconstruction
    ///     stopwatch.Restart();
    ///     var fullResult = await _context.GetInMemoryAggregate(streamId, aggregateIdObj);
    ///     stopwatch.Stop();
    ///     metrics.FullReconstructionTime = stopwatch.Elapsed;
    ///     
    ///     return metrics;
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<TAggregate>> GetAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, bool applyNewDomainEvents = false, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            throw new InvalidOperationException($"Aggregate {typeof(TAggregate).Name} does not have a AggregateType attribute.");
        }

        var aggregateEntity = await domainDbContext.Aggregates.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == aggregateId.ToIdWithTypeVersion(aggregateType.Version), cancellationToken);
        if (aggregateEntity is not null)
        {
            var currentAggregate = aggregateEntity.ToAggregate<TAggregate>();
            if (!applyNewDomainEvents)
            {
                return currentAggregate;
            }
            return await domainDbContext.UpdateAggregate(streamId, aggregateId, currentAggregate, cancellationToken);
        }

        var aggregate = new TAggregate();

        var eventEntities = await domainDbContext.GetEventEntities(streamId, aggregate.EventTypeFilter, cancellationToken);
        if (eventEntities.Count == 0)
        {
            return aggregate;
        }

        var domainEvents = eventEntities.Select(eventEntity => eventEntity.ToDomainEvent()).ToList();
        aggregate.Apply(domainEvents);

        if (aggregate.Version == 0)
        {
            return aggregate;
        }

        var latestEventSequenceForAggregate = eventEntities.OrderBy(eventEntity => eventEntity.Sequence).Last().Sequence;
        var trackedAggregateEntity = domainDbContext.TrackAggregateEntity(streamId, aggregateId, aggregate, latestEventSequenceForAggregate, aggregateIsNew: true);
        domainDbContext.TrackAggregateEventEntities(trackedAggregateEntity, eventEntities);

        try
        {
            await domainDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Get Aggregate");
            return ErrorHandling.DefaultFailure;
        }

        domainDbContext.DetachAggregate(aggregateId, aggregate);

        return aggregate;
    }
}
