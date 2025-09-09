using System.Diagnostics;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extension methods for diagnostics in the Entity Framework Core store.
/// </summary>
public static class DiagnosticsExtensions
{
    /// <summary>
    /// Adds an activity event for a concurrency exception.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="expectedEventSequence">The expected event sequence.</param>
    /// <param name="latestEventSequence">The latest event sequence.</param>
    public static void AddActivityEvent(IStreamId streamId, int expectedEventSequence, int latestEventSequence)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "Concurrency exception", timestamp: default, tags: new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "expectedEventSequence", expectedEventSequence },
            { "latestEventSequence", latestEventSequence }
        }));
    }

    /// <summary>
    /// Adds an exception to the current activity with stream ID and operation description.
    /// </summary>
    /// <param name="exception">The exception to add.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="operationDescription">The description of the operation.</param>
    public static void AddException(this Exception exception, IStreamId streamId, string operationDescription)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "streamId", streamId.Id },
            { "operation", operationDescription }
        });
    }

    /// <summary>
    /// Adds an exception to the current activity with operation description.
    /// </summary>
    /// <param name="exception">The exception to add.</param>
    /// <param name="operationDescription">The description of the operation.</param>
    public static void AddException(this Exception exception, string operationDescription)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "operation", operationDescription }
        });
    }
}
