using System.Diagnostics;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;

public static partial class IDomainDbContextExtensions
{
    /// <summary>
    /// Saves all pending changes in the domain database context to the underlying data store.
    /// This method provides a simple way to persist tracked entity changes without additional
    /// event sourcing logic, suitable for scenarios where entities have been explicitly tracked.
    /// </summary>
    /// <param name="domainDbContext">
    /// The domain database context containing tracked entity changes that need to be persisted
    /// to the database through Entity Framework Core's change tracking mechanism.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the asynchronous operation if needed,
    /// supporting graceful shutdown and timeout scenarios.
    /// </param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the save operation. On success,
    /// returns <see cref="Result.Ok()"/>. On failure, returns a <see cref="Failure"/> with detailed
    /// error information about any persistence exceptions that occurred.
    /// </returns>
    /// <example>
    /// <code>
    /// // Bulk entity updates with final save
    /// public async Task&lt;Result&gt; UpdateMultipleAggregatesAsync(List&lt;UpdateAggregateRequest&gt; requests)
    /// {
    ///     try
    ///     {
    ///         foreach (var request in requests)
    ///         {
    ///             var streamId = new StreamId($"aggregate-{request.AggregateId}");
    ///             var aggregateId = new GenericAggregateId(request.AggregateId);
    ///             
    ///             // Track changes without individual saves
    ///             var trackResult = await _context.Track(streamId, request.Events.ToArray(), request.ExpectedSequence);
    ///             if (trackResult.IsNotSuccess)
    ///                 return trackResult.Failure!;
    ///         }
    ///         
    ///         // Save all changes at once
    ///         return await _context.Save();
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         return new Failure("Bulk update failed", ex.Message);
    ///     }
    /// }
    /// 
    /// // Integration with existing Entity Framework code
    /// public async Task&lt;Result&gt; MigrateDataAsync()
    /// {
    ///     // Use direct EF operations for migration
    ///     var legacyEntities = await _context.Events.Where(e =&gt; e.TypeVersion &lt; 2).ToListAsync();
    ///     
    ///     foreach (var entity in legacyEntities)
    ///     {
    ///         // Update entity properties directly
    ///         entity.TypeVersion = 2;
    ///         entity.Data = MigrateEventData(entity.Data);
    ///         
    ///         _context.Events.Update(entity);
    ///     }
    ///     
    ///     // Save all migrations
    ///     return await _context.Save();
    /// }
    /// 
    /// // Multi-step operation with intermediate save
    /// public async Task&lt;Result&gt; ProcessComplexWorkflowAsync(WorkflowRequest request)
    /// {
    ///     // Step 1: Prepare initial state
    ///     var prepareResult = await PrepareWorkflowState(request);
    ///     if (prepareResult.IsNotSuccess)
    ///         return prepareResult;
    ///         
    ///     // Save intermediate state
    ///     var saveResult = await _context.Save();
    ///     if (saveResult.IsNotSuccess)
    ///         return saveResult;
    ///     
    ///     // Step 2: Process workflow steps
    ///     foreach (var step in request.Steps)
    ///     {
    ///         var stepResult = await ProcessWorkflowStep(step);
    ///         if (stepResult.IsNotSuccess)
    ///             return stepResult;
    ///     }
    ///     
    ///     // Final save
    ///     return await _context.Save();
    /// }
    /// 
    /// // Simple repository pattern integration
    /// public class GenericRepository&lt;T&gt; where T : class
    /// {
    ///     private readonly IDomainDbContext _context;
    ///     
    ///     public GenericRepository(IDomainDbContext context)
    ///     {
    ///         _context = context;
    ///     }
    ///     
    ///     public async Task&lt;Result&gt; AddAsync(T entity)
    ///     {
    ///         _context.Set&lt;T&gt;().Add(entity);
    ///         return await _context.Save();
    ///     }
    ///     
    ///     public async Task&lt;Result&gt; UpdateAsync(T entity)
    ///     {
    ///         _context.Set&lt;T&gt;().Update(entity);
    ///         return await _context.Save();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<Result> Save(this IDomainDbContext domainDbContext, CancellationToken cancellationToken = default)
    {
        try
        {
            await domainDbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            ex.AddException(operationDescription: "Save");
            return ErrorHandling.DefaultFailure;
        }
    }
}
