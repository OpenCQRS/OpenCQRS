using System.Diagnostics;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Saves an array of domain events directly to the event store with optimistic concurrency control,
    /// bypassing aggregate persistence. This method is ideal for scenarios where events are generated
    /// outside traditional aggregate workflows, such as integration events or system-generated events.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context that provides access to event store entities and change tracking
    /// functionality for Entity Framework Core operations.
    /// </param>
    /// <param name="streamId">
    /// The unique identifier for the event stream where these events will be appended. Events will
    /// be ordered sequentially within this stream starting from the expected sequence position.
    /// </param>
    /// <param name="domainEvents">
    /// An array of domain events implementing <see cref="IDomainEvent"/> to be persisted to the event store.
    /// Each event will be serialized and stored as an individual <see cref="EventEntity"/> with proper
    /// sequence numbering and type information.
    /// </param>
    /// <param name="expectedEventSequence">
    /// The expected sequence number of the last event currently in the stream, used for optimistic
    /// concurrency control. New events will be appended starting from this sequence + 1.
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
    /// // Save integration events from external system
    /// public async Task&lt;Result&gt; PublishIntegrationEventsAsync(
    ///     IStreamId streamId, 
    ///     IEnumerable&lt;IDomainEvent&gt; integrationEvents)
    /// {
    ///     var events = integrationEvents.ToArray();
    ///     if (events.Length == 0)
    ///         return Result.Ok();
    ///         
    ///     var currentSequence = await _context.GetLatestEventSequence(streamId);
    ///     return await _context.Save(streamId, events, currentSequence);
    /// }
    /// 
    /// // Saga coordination events
    /// public class OrderProcessingSaga
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; HandlePaymentProcessedAsync(PaymentProcessedEvent paymentEvent)
    ///     {
    ///         var sagaStreamId = new StreamId($"order-processing-saga-{paymentEvent.OrderId}");
    ///         
    ///         // Create saga coordination events
    ///         var sagaEvents = new IDomainEvent[]
    ///         {
    ///             new PaymentConfirmedEvent(paymentEvent.OrderId, paymentEvent.Amount),
    ///             new InventoryReservationRequestedEvent(paymentEvent.OrderId, paymentEvent.Items)
    ///         };
    ///         
    ///         var currentSequence = await _context.GetLatestEventSequence(sagaStreamId);
    ///         return await _context.Save(sagaStreamId, sagaEvents, currentSequence);
    ///     }
    /// }
    /// 
    /// // System audit events
    /// public class AuditEventHandler : IRequestHandler&lt;RecordAuditEventRequest&gt;
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public async Task&lt;Result&gt; Handle(RecordAuditEventRequest request, CancellationToken cancellationToken)
    ///     {
    ///         var auditStreamId = new StreamId($"audit-{request.EntityType}-{request.EntityId}");
    ///         
    ///         var auditEvents = new IDomainEvent[]
    ///         {
    ///             new EntityAccessedEvent(request.EntityId, request.UserId, request.Timestamp),
    ///             new UserActionRecordedEvent(request.UserId, request.Action, request.Timestamp)
    ///         };
    ///         
    ///         var currentSequence = await _context.GetLatestEventSequence(auditStreamId, cancellationToken: cancellationToken);
    ///         return await _context.Save(auditStreamId, auditEvents, currentSequence, cancellationToken);
    ///     }
    /// }
    /// 
    /// // Event replay scenario
    /// public async Task&lt;Result&gt; ReplayEventsAsync(IStreamId sourceStreamId, IStreamId targetStreamId)
    /// {
    ///     var eventsToReplay = await _context.GetDomainEvents(sourceStreamId);
    ///     var targetSequence = await _context.GetLatestEventSequence(targetStreamId);
    ///     
    ///     return await _context.Save(targetStreamId, eventsToReplay.ToArray(), targetSequence);
    /// }
    /// </code>
    /// </example>
    public static async Task<Result> SaveDomainEvents(this IDomainDbContext domainDbContext, IStreamId streamId, IDomainEvent[] domainEvents, int expectedEventSequence, CancellationToken cancellationToken = default)
    {
        try
        {
            var trackResult = await domainDbContext.TrackDomainEvents(streamId, domainEvents, expectedEventSequence, cancellationToken);
            if (trackResult.IsNotSuccess)
            {
                return trackResult.Failure!;
            }

            await domainDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            ex.AddException(streamId, operationDescription: "Save Domain Events");
            return ErrorHandling.DefaultFailure;
        }
    }
}
