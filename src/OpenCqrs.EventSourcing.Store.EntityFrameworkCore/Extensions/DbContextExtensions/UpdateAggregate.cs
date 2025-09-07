using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Updates an existing aggregate with new events from its stream, applying any events that occurred
    /// after the aggregate's last known state. This method enables aggregate synchronization with the
    /// latest event stream state while maintaining proper version control and event application ordering.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate to update. Must implement <see cref="IAggregate"/> and have a parameterless
    /// constructor to support aggregate reconstruction from stored state and new events.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event store entities and aggregate snapshots
    /// for retrieving current state and applying new events.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream containing events that may need to be applied to
    /// the aggregate to bring it up to the current state.
    /// </param>
    /// <param name="aggregateId">
    /// The strongly-typed unique identifier for the aggregate to update. Used to locate the existing
    /// aggregate snapshot and determine which events are applicable.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result{TAggregate}"/> containing either the updated aggregate with the latest
    /// state applied from new events, or a <see cref="Failure"/> if the update operation failed
    /// due to missing aggregate type metadata or other persistence issues.
    /// </returns>
    /// <example>
    /// <code>
    /// // Update aggregate in command handler
    /// public class UpdateOrderCommandHandler : IRequestHandler&lt;UpdateOrderCommand&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; Handle(UpdateOrderCommand command, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new StreamId($"order-stream-{command.OrderId}");
    ///         var aggregateId = new OrderAggregateId(command.OrderId);
    ///         
    ///         // Get aggregate with latest events applied
    ///         var aggregateResult = await _context.UpdateAggregate(streamId, aggregateId, cancellationToken);
    ///         if (aggregateResult.IsNotSuccess)
    ///             return aggregateResult.Failure!;
    ///             
    ///         var order = aggregateResult.Value!;
    ///         
    ///         // Apply business logic changes
    ///         order.UpdateShippingAddress(command.NewAddress);
    ///         order.AddNote(command.UpdateNote);
    ///         
    ///         // Save changes with current sequence
    ///         var currentSequence = await _context.GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
    ///         return await _context.Save(streamId, aggregateId, order, currentSequence, cancellationToken);
    ///     }
    /// }
    /// 
    /// // Read-model projection with aggregate updates
    /// public class OrderProjectionHandler : IRequestHandler&lt;ProjectOrderViewRequest&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&lt;OrderView&gt;&gt; Handle(ProjectOrderViewRequest request, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new StreamId($"order-{request.OrderId}");
    ///         var aggregateId = new OrderAggregateId(request.OrderId);
    ///         
    ///         // Ensure aggregate is up-to-date with latest events
    ///         var aggregateResult = await _context.UpdateAggregate(streamId, aggregateId, cancellationToken);
    ///         if (aggregateResult.IsNotSuccess)
    ///             return aggregateResult.Failure!;
    ///             
    ///         var order = aggregateResult.Value!;
    ///         
    ///         // Project to view model
    ///         return new OrderView
    ///         {
    ///             OrderId = order.Id,
    ///             Status = order.Status,
    ///             Items = order.Items.Select(item =&gt; new OrderItemView
    ///             {
    ///                 ProductId = item.ProductId,
    ///                 Quantity = item.Quantity,
    ///                 UnitPrice = item.UnitPrice
    ///             }).ToList(),
    ///             LastModified = order.LastModifiedAt
    ///         };
    ///     }
    /// }
    /// 
    /// // Aggregate synchronization service
    /// public class AggregateSynchronizationService
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; SynchronizeAggregateAsync&lt;T&gt;(
    ///         IStreamId streamId, 
    ///         IAggregateId&lt;T&gt; aggregateId) where T : IAggregate, new()
    ///     {
    ///         // Get synchronized aggregate
    ///         var aggregateResult = await _context.UpdateAggregate(streamId, aggregateId);
    ///         if (aggregateResult.IsNotSuccess)
    ///             return aggregateResult.Failure!;
    ///             
    ///         var aggregate = aggregateResult.Value!;
    ///         
    ///         // Aggregate is now synchronized with latest events
    ///         return Result.Ok();
    ///     }
    ///     
    ///     public async Task&lt;Result&gt; SynchronizeMultipleAggregatesAsync&lt;T&gt;(
    ///         Dictionary&lt;IStreamId, IAggregateId&lt;T&gt;&gt; aggregateMap) where T : IAggregate, new()
    ///     {
    ///         foreach (var kvp in aggregateMap)
    ///         {
    ///             var result = await SynchronizeAggregateAsync(kvp.Key, kvp.Value);
    ///             if (result.IsNotSuccess)
    ///                 return result;
    ///         }
    ///         
    ///         return Result.Ok();
    ///     }
    /// }
    /// 
    /// // Aggregate refresh in long-running processes
    /// public class LongRunningProcessManager
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     private readonly Timer _refreshTimer;
    ///     
    ///     public async Task RefreshAggregatesAsync()
    ///     {
    ///         var aggregatesToRefresh = GetTrackedAggregates();
    ///         
    ///         foreach (var (streamId, aggregateId) in aggregatesToRefresh)
    ///         {
    ///             var refreshResult = await _context.UpdateAggregate(streamId, aggregateId);
    ///             if (refreshResult.IsSuccess)
    ///             {
    ///                 UpdateLocalCache(aggregateId, refreshResult.Value!);
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<Result<TAggregate>> UpdateAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId<TAggregate> aggregateId, CancellationToken cancellationToken = default) where TAggregate : IAggregate, new()
    {
        var aggregateType = typeof(TAggregate).GetCustomAttribute<AggregateType>();
        if (aggregateType is null)
        {
            throw new InvalidOperationException($"Aggregate {typeof(TAggregate).Name} does not have a AggregateType attribute.");
        }

        var aggregateEntity = await domainDbContext.Aggregates.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == aggregateId.ToIdWithTypeVersion(aggregateType.Version), cancellationToken);
        if (aggregateEntity is null)
        {
            return new TAggregate();
        }

        var aggregate = aggregateEntity.ToAggregate<TAggregate>();

        return await domainDbContext.UpdateAggregate(streamId, aggregateId, aggregate, cancellationToken);
    }
}
