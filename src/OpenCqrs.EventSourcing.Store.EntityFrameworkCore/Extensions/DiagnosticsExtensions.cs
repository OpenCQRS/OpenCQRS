using System.Diagnostics;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

public static class DiagnosticsExtensions
{
    public static void AddActivityEvent(IStreamId streamId, int expectedEventSequence, int latestEventSequence)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name: "Concurrency exception", timestamp: default, tags: new ActivityTagsCollection
        {
            { "streamId", streamId.Id },
            { "expectedEventSequence", expectedEventSequence },
            { "latestEventSequence", latestEventSequence }
        }));
    }

    public static void AddException(this Exception exception, IStreamId streamId, string operationDescription)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "streamId", streamId.Id },
            { "operation", operationDescription }
        });
    }

    public static void AddException(this Exception exception, string operationDescription)
    {
        Activity.Current?.AddException(exception, tags: new TagList
        {
            { "operation", operationDescription }
        });
    }
}
