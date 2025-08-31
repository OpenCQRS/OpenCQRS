// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

/// <summary>
/// Represents a many-to-many relationship entity that links aggregates to their associated domain events
/// in the Entity Framework Core event store. This junction table enables efficient querying of events
/// by aggregate and supports complex event sourcing scenarios.
/// </summary>
/// <example>
/// <code>
/// // Entity Framework configuration
/// public void Configure(EntityTypeBuilder&lt;AggregateEventEntity&gt; builder)
/// {
///     builder.HasKey(ae =&gt; new { ae.AggregateId, ae.EventId });
///     
///     builder.HasOne(ae =&gt; ae.Aggregate)
///         .WithMany()
///         .HasForeignKey(ae =&gt; ae.AggregateId)
///         .OnDelete(DeleteBehavior.Cascade);
///     
///     builder.HasOne(ae =&gt; ae.Event)
///         .WithMany()
///         .HasForeignKey(ae =&gt; ae.EventId)
///         .OnDelete(DeleteBehavior.Cascade);
///         
///     builder.HasIndex(ae =&gt; ae.AggregateId);
///     builder.HasIndex(ae =&gt; ae.EventId);
/// }
/// 
/// // Usage in queries
/// public async Task&lt;List&lt;EventEntity&gt;&gt; GetAggregateEventsAsync(string aggregateId)
/// {
///     return await _context.AggregateEvents
///         .Where(ae =&gt; ae.AggregateId == aggregateId)
///         .Include(ae =&gt; ae.Event)
///         .Select(ae =&gt; ae.Event)
///         .OrderBy(e =&gt; e.Sequence)
///         .ToListAsync();
/// }
/// 
/// // Finding affected aggregates
/// public async Task&lt;List&lt;string&gt;&gt; GetAffectedAggregatesAsync(string eventId)
/// {
///     return await _context.AggregateEvents
///         .Where(ae =&gt; ae.EventId == eventId)
///         .Select(ae =&gt; ae.AggregateId)
///         .ToListAsync();
/// }
/// 
/// // Bulk loading for aggregate reconstruction
/// public async Task&lt;Dictionary&lt;string, List&lt;IDomainEvent&gt;&gt;&gt; LoadAggregateEventsAsync(List&lt;string&gt; aggregateIds)
/// {
///     var aggregateEvents = await _context.AggregateEvents
///         .Where(ae =&gt; aggregateIds.Contains(ae.AggregateId))
///         .Include(ae =&gt; ae.Event)
///         .ToListAsync();
///     
///     return aggregateEvents
///         .GroupBy(ae =&gt; ae.AggregateId)
///         .ToDictionary(
///             g =&gt; g.Key,
///             g =&gt; g.Select(ae =&gt; ae.Event.ToDomainEvent())
///                   .OrderBy(e =&gt; ae.Event.Sequence)
///                   .ToList()
///         );
/// }
/// </code>
/// </example>
public class AggregateEventEntity : IApplicableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the aggregate that owns the associated event.
    /// This forms part of the composite primary key and serves as a foreign key to <see cref="AggregateEntity"/>.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies the aggregate instance. Must match the 
    /// <see cref="AggregateEntity.Id"/> of an existing aggregate in the database.
    /// </value>
    public string AggregateId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the domain event associated with the aggregate.
    /// This forms part of the composite primary key and serves as a foreign key to <see cref="EventEntity"/>.
    /// </summary>
    /// <value>
    /// A string that uniquely identifies the domain event instance. Must match the 
    /// <see cref="EventEntity.Id"/> of an existing event in the database.
    /// </value>
    public string EventId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp indicating when this domain event was applied to the aggregate instance.
    /// This property tracks the exact moment when the event-aggregate relationship was established,
    /// providing crucial audit information for event application timing and chronological ordering.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing the UTC timestamp when the event was applied to the aggregate.
    /// This timestamp is typically set during the aggregate tracking process and represents when the event
    /// became part of the aggregate's state history, not necessarily when the event originally occurred.
    /// </value>
    /// <remarks>
    /// The applied date differs from the event's creation date in that it represents when the event
    /// was specifically associated with this aggregate instance. In scenarios where events are replayed
    /// or aggregates are reconstructed, this timestamp provides insights into the event application timeline.
    /// This property is essential for audit trails, debugging event application sequences, and understanding
    /// the temporal relationship between events and aggregate state changes.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Setting applied date during event tracking
    /// public AggregateEventEntity CreateAggregateEventRelationship(
    ///     string aggregateId, 
    ///     string eventId)
    /// {
    ///     return new AggregateEventEntity
    ///     {
    ///         AggregateId = aggregateId,
    ///         EventId = eventId,
    ///         AppliedDate = DateTimeOffset.UtcNow // Set when relationship is created
    ///     };
    /// }
    /// 
    /// // Querying events by application time range
    /// public async Task&lt;List&lt;EventEntity&gt;&gt; GetEventsAppliedInPeriodAsync(
    ///     string aggregateId,
    ///     DateTimeOffset fromDate,
    ///     DateTimeOffset toDate)
    /// {
    ///     return await _context.AggregateEvents
    ///         .Where(ae =&gt; ae.AggregateId == aggregateId 
    ///                   &amp;&amp; ae.AppliedDate &gt;= fromDate 
    ///                   &amp;&amp; ae.AppliedDate &lt;= toDate)
    ///         .Include(ae =&gt; ae.Event)
    ///         .Select(ae =&gt; ae.Event)
    ///         .OrderBy(e =&gt; e.Sequence)
    ///         .ToListAsync();
    /// }
    /// 
    /// // Audit trail generation
    /// public async Task&lt;AuditTrail&gt; GenerateEventApplicationAuditAsync(string aggregateId)
    /// {
    ///     var applicationHistory = await _context.AggregateEvents
    ///         .Where(ae =&gt; ae.AggregateId == aggregateId)
    ///         .Include(ae =&gt; ae.Event)
    ///         .OrderBy(ae =&gt; ae.AppliedDate)
    ///         .Select(ae =&gt; new EventApplicationRecord
    ///         {
    ///             EventId = ae.EventId,
    ///             EventType = ae.Event.TypeName,
    ///             EventSequence = ae.Event.Sequence,
    ///             AppliedAt = ae.AppliedDate,
    ///             ApplicationOrder = ae.AppliedDate // Shows chronological application order
    ///         })
    ///         .ToListAsync();
    /// 
    ///     return new AuditTrail
    ///     {
    ///         AggregateId = aggregateId,
    ///         EventApplications = applicationHistory,
    ///         FirstApplicationDate = applicationHistory.FirstOrDefault()?.AppliedAt,
    ///         LastApplicationDate = applicationHistory.LastOrDefault()?.AppliedAt,
    ///         TotalEventsApplied = applicationHistory.Count
    ///     };
    /// }
    /// 
    /// // Performance analysis of event application timing
    /// public async Task&lt;EventApplicationMetrics&gt; AnalyzeApplicationTimingAsync(string aggregateId)
    /// {
    ///     var applicationTimes = await _context.AggregateEvents
    ///         .Where(ae =&gt; ae.AggregateId == aggregateId)
    ///         .Include(ae =&gt; ae.Event)
    ///         .OrderBy(ae =&gt; ae.Event.Sequence)
    ///         .Select(ae =&gt; new { ae.AppliedDate, ae.Event.CreatedDate })
    ///         .ToListAsync();
    /// 
    ///     var applicationDelays = applicationTimes
    ///         .Select(at =&gt; at.AppliedDate - at.CreatedDate)
    ///         .ToList();
    /// 
    ///     return new EventApplicationMetrics
    ///     {
    ///         AggregateId = aggregateId,
    ///         AverageApplicationDelay = TimeSpan.FromMilliseconds(
    ///             applicationDelays.Average(d =&gt; d.TotalMilliseconds)),
    ///         MaxApplicationDelay = applicationDelays.Max(),
    ///         MinApplicationDelay = applicationDelays.Min(),
    ///         EventsWithImmediateApplication = applicationDelays.Count(d =&gt; d.TotalSeconds &lt; 1),
    ///         EventsWithDelayedApplication = applicationDelays.Count(d =&gt; d.TotalMinutes &gt; 1)
    ///     };
    /// }
    /// 
    /// // Debugging event application sequences
    /// public async Task&lt;List&lt;EventApplicationDebugInfo&gt;&gt; GetEventApplicationTimelineAsync(
    ///     string aggregateId)
    /// {
    ///     return await _context.AggregateEvents
    ///         .Where(ae =&gt; ae.AggregateId == aggregateId)
    ///         .Include(ae =&gt; ae.Event)
    ///         .Include(ae =&gt; ae.Aggregate)
    ///         .OrderBy(ae =&gt; ae.AppliedDate) // Chronological application order
    ///         .Select(ae =&gt; new EventApplicationDebugInfo
    ///         {
    ///             EventId = ae.EventId,
    ///             EventType = ae.Event.TypeName,
    ///             EventSequence = ae.Event.Sequence,
    ///             EventCreated = ae.Event.CreatedDate,
    ///             AppliedToAggregate = ae.AppliedDate,
    ///             ApplicationDelay = ae.AppliedDate - ae.Event.CreatedDate,
    ///             AggregateVersion = ae.Aggregate.Version,
    ///             IsOutOfSequenceApplication = ae.AppliedDate != ae.Event.CreatedDate
    ///         })
    ///         .ToListAsync();
    /// }
    /// 
    /// // Finding recently applied events
    /// public async Task&lt;List&lt;string&gt;&gt; GetRecentlyAppliedEventIdsAsync(
    ///     string aggregateId, 
    ///     TimeSpan timeWindow)
    /// {
    ///     var cutoffTime = DateTimeOffset.UtcNow - timeWindow;
    ///     
    ///     return await _context.AggregateEvents
    ///         .Where(ae =&gt; ae.AggregateId == aggregateId 
    ///                   &amp;&amp; ae.AppliedDate &gt;= cutoffTime)
    ///         .OrderByDescending(ae =&gt; ae.AppliedDate)
    ///         .Select(ae =&gt; ae.EventId)
    ///         .ToListAsync();
    /// }
    /// 
    /// // Bulk event application timestamp update
    /// public async Task&lt;Result&gt; UpdateApplicationTimestampsAsync(
    ///     Dictionary&lt;string, DateTimeOffset&gt; eventApplicationTimes)
    /// {
    ///     foreach (var kvp in eventApplicationTimes)
    ///     {
    ///         var aggregateEvent = await _context.AggregateEvents
    ///             .FirstOrDefaultAsync(ae =&gt; ae.EventId == kvp.Key);
    ///             
    ///         if (aggregateEvent != null)
    ///         {
    ///             aggregateEvent.AppliedDate = kvp.Value;
    ///             _context.AggregateEvents.Update(aggregateEvent);
    ///         }
    ///     }
    ///     
    ///     try
    ///     {
    ///         await _context.SaveChangesAsync();
    ///         return Result.Ok();
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         return new Failure("Failed to update application timestamps", ex.Message);
    ///     }
    /// }
    /// </code>
    /// </example>
    public DateTimeOffset AppliedDate { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated aggregate entity.
    /// Enables Entity Framework to automatically load aggregate data when querying through this relationship.
    /// </summary>
    /// <value>
    /// An <see cref="AggregateEntity"/> instance representing the aggregate that owns the associated event.
    /// This property is virtual to support Entity Framework lazy loading and proxy generation.
    /// </value>
    public virtual AggregateEntity Aggregate { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the associated domain event entity.
    /// Enables Entity Framework to automatically load event data when querying through this relationship.
    /// </summary>
    /// <value>
    /// An <see cref="EventEntity"/> instance representing the domain event associated with the aggregate.
    /// This property is virtual to support Entity Framework lazy loading and proxy generation.
    /// </value>
    public virtual EventEntity Event { get; set; } = null!;
}
