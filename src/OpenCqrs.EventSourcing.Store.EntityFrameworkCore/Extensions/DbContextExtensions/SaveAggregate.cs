using System.Diagnostics;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Saves an aggregate to the event store with optimistic concurrency control, persisting all uncommitted
    /// domain events and updating the aggregate snapshot. This method provides comprehensive event sourcing
    /// persistence with automatic error handling, activity tracing, and memory optimization.
    /// </summary>
    /// <typeparam name="TAggregate">
    /// The type of the aggregate being saved. Must implement <see cref="IAggregate"/> to ensure proper
    /// event sourcing contract compliance, including uncommitted events tracking and version management.
    /// </typeparam>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event store entities and change tracking
    /// functionality for Entity Framework Core operations.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream associated with this aggregate. Used to maintain
    /// event ordering and enable stream-based queries across related aggregates and bounded contexts.
    /// </param>
    /// <param name="aggregateId">
    /// The unique identifier for the aggregate being saved. Combined with type version information
    /// to ensure accurate aggregate identification and prevent cross-type conflicts.
    /// </param>
    /// <param name="aggregate">
    /// The aggregate instance containing the business state and uncommitted domain events to persist.
    /// The aggregate's uncommitted events will be serialized and stored as individual event entities.
    /// </param>
    /// <param name="expectedEventSequence">
    /// The expected sequence number of the last event in the stream, used for optimistic concurrency
    /// control. If the actual sequence doesn't match, a concurrency exception will be returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the save operation. On success,
    /// returns <see cref="Result.Ok()"/>. On failure, returns a <see cref="Failure"/> with detailed
    /// error information including concurrency violations and persistence exceptions.
    /// </returns>
    /// <example>
    /// <code>
    /// // Save a new order aggregate
    /// public async Task&lt;Result&gt; CreateOrderAsync(CreateOrderRequest request)
    /// {
    ///     var streamId = new StreamId($"order-stream-{request.CustomerId}");
    ///     var aggregateId = new OrderAggregateId(Guid.NewGuid());
    ///     
    ///     var order = new OrderAggregate(aggregateId.Value, request.CustomerId);
    ///     order.AddLineItem(request.ProductId, request.Quantity, request.UnitPrice);
    ///     order.SetShippingAddress(request.ShippingAddress);
    ///     
    ///     // Save with expected sequence of 0 for new aggregate
    ///     var result = await _context.Save(streamId, aggregateId, order, expectedEventSequence: 0);
    ///     
    ///     return result;
    /// }
    /// 
    /// // Update existing aggregate with concurrency control
    /// public async Task&lt;Result&gt; UpdateOrderAsync(Guid orderId, UpdateOrderRequest request)
    /// {
    ///     var streamId = new StreamId($"order-stream-{orderId}");
    ///     var aggregateId = new OrderAggregateId(orderId);
    ///     
    ///     // Get current aggregate state
    ///     var aggregateResult = await _context.GetAggregate(streamId, aggregateId);
    ///     if (aggregateResult.IsNotSuccess)
    ///         return aggregateResult.Failure!;
    ///         
    ///     var order = aggregateResult.Value!;
    ///     var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///     
    ///     // Apply business logic changes
    ///     order.UpdateShippingAddress(request.NewAddress);
    ///     order.AddLineItem(request.ProductId, request.Quantity, request.UnitPrice);
    ///     
    ///     // Save with current sequence for concurrency control
    ///     return await _context.Save(streamId, aggregateId, order, currentSequence);
    /// }
    /// 
    /// // Integration with command handler pattern
    /// public class ProcessOrderCommandHandler : IRequestHandler&lt;ProcessOrderCommand&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public ProcessOrderCommandHandler(IDomainDbContext context)
    ///     {
    ///         _context = context;
    ///     }
    ///     
    ///     public async Task&lt;Result&gt; Handle(ProcessOrderCommand command, CancellationToken cancellationToken)
    ///     {
    ///         var streamId = new StreamId($"order-{command.OrderId}");
    ///         var aggregateId = new OrderAggregateId(command.OrderId);
    ///         
    ///         var aggregateResult = await _context.GetAggregate(streamId, aggregateId, cancellationToken);
    ///         if (aggregateResult.IsNotSuccess)
    ///             return aggregateResult.Failure!;
    ///             
    ///         var order = aggregateResult.Value!;
    ///         var expectedSequence = await _context.GetLatestEventSequence(streamId, cancellationToken: cancellationToken);
    ///         
    ///         // Apply business logic
    ///         order.Process(command.ProcessingNotes);
    ///         
    ///         // Save changes with concurrency control
    ///         return await _context.Save(streamId, aggregateId, order, expectedSequence, cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<Result> SaveAggregate<TAggregate>(this IDomainDbContext domainDbContext, IStreamId streamId, IAggregateId aggregateId, TAggregate aggregate, int expectedEventSequence, CancellationToken cancellationToken = default) where TAggregate : IAggregate
    {
        try
        {
            var trackResult = await domainDbContext.TrackWithAggregate(streamId, aggregateId, aggregate, expectedEventSequence, cancellationToken);
            if (trackResult.IsNotSuccess)
            {
                return trackResult.Failure!;
            }

            await domainDbContext.SaveChangesAsync(cancellationToken);

            domainDbContext.DetachAggregate(aggregateId, aggregate);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var tags = new Dictionary<string, object> { { "Message", ex.Message } };
            Activity.Current?.AddEvent(new ActivityEvent("There was an error when saving the aggregate", tags: new ActivityTagsCollection(tags!)));
            return new Failure
            (
                Title: "Error saving changes",
                Description: "There was an error when saving the aggregate"
            );
        }
    }
}
