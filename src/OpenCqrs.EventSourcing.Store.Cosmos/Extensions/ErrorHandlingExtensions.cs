using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

public static class ErrorHandlingExtensions
{
    public static Failure ToFailure(this TransactionalBatchResponse batchResponse, string operationDescription)
    {
        // TODO: Add more tags from the response
        var tags = new Dictionary<string, object> { { "Message", batchResponse.ErrorMessage } };
        Activity.Current?.AddEvent(new ActivityEvent($"{operationDescription}: batch execution failed", tags: new ActivityTagsCollection(tags!)));
        return new Failure
        (
            Title: operationDescription,
            Description: "There was an error when processing the request"
        );
    }

    public static Failure ToFailure(this Exception exception, string operationDescription)
    {
        // TODO: Add more tags from the exception
        var tags = new Dictionary<string, object> { { "Message", exception.Message } };
        Activity.Current?.AddEvent(new ActivityEvent($"{operationDescription}: exception", tags: new ActivityTagsCollection(tags!)));
        return new Failure
        (
            Title: operationDescription,
            Description: "There was an error when processing the request"
        );
    }
}
