using System.Diagnostics;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions;

public static class ErrorHandling
{
    public static Failure ProcessErrorAndGetFailure(this Exception exception, string operationDescription)
    {
        var tagList = new TagList { { "Operation Description", operationDescription } };
        Activity.Current?.AddException(exception, tagList, DateTimeOffset.UtcNow);
        return new Failure
        (
            Title: operationDescription,
            Description: "There was an error when processing the request"
        );
    }

    public static Failure ProcessErrorAndGetFailure(int expectedEventSequence, int latestEventSequence)
    {
        var tags = new Dictionary<string, object?>
        {
            { "ExpectedEventSequence", expectedEventSequence },
            { "LatestEventSequence", latestEventSequence }
        };

        Activity.Current?.AddEvent(new ActivityEvent(
            name: "Concurrency exception",
            tags: new ActivityTagsCollection(tags)));

        return new Failure
        (
            Title: "Error",
            Description: "There was an error when processing the request"
        );
    }
}
