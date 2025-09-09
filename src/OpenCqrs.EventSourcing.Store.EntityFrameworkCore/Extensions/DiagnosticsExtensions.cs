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
        Activity.Current?.AddEvent(new ActivityEvent(name: "Concurrency Exception", timestamp: default, tags: new ActivityTagsCollection
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
    /// <param name="operation">The description of the operation.</param>
    public static void AddException(this Exception exception, IStreamId streamId, string operation)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "operation", operation },
            { "streamId", streamId.Id }
        });
    }

    /// <summary>
    /// Adds an exception to the current activity with the operation description.
    /// </summary>
    /// <param name="exception">The exception to add.</param>
    /// <param name="operation">The description of the operation.</param>
    public static void AddException(this Exception exception, string operation)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "operation", operation }
        });
    }
}
