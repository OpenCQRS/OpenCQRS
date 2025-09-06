using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

/// <summary>
/// Provides utility methods for processing errors and creating failure responses in the Cosmos DB Event Sourcing store.
/// These methods standardize error handling and activity logging across the store operations.
/// </summary>
public static class ErrorHandling
{
    /// <summary>
    /// Processes a failed transactional batch response and creates a standardized failure result.
    /// This method logs the error details to the current activity and returns a user-friendly failure response.
    /// </summary>
    /// <param name="batchResponse">The failed transactional batch response from Cosmos DB.</param>
    /// <param name="operationDescription">A description of the operation that failed.</param>
    /// <returns>A <see cref="Failure"/> object containing standardized error information.</returns>
    public static Failure ProcessErrorAndGetFailure(this TransactionalBatchResponse batchResponse, string operationDescription)
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

    /// <summary>
    /// Processes an exception and creates a standardized failure result.
    /// This method logs the exception details to the current activity and returns a user-friendly failure response.
    /// </summary>
    /// <param name="exception">The exception that occurred during the operation.</param>
    /// <param name="operationDescription">A description of the operation that failed.</param>
    /// <returns>A <see cref="Failure"/> object containing standardized error information.</returns>
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

    /// <summary>
    /// Processes a concurrency conflict error and creates a standardized failure result.
    /// This method logs the concurrency conflict details to the current activity when event sequences don't match expectations.
    /// </summary>
    /// <param name="expectedEventSequence">The expected event sequence number.</param>
    /// <param name="latestEventSequence">The actual latest event sequence number found in the store.</param>
    /// <param name="timestamp">The timestamp when the concurrency conflict was detected.</param>
    /// <returns>A <see cref="Failure"/> object containing standardized error information.</returns>
    public static Failure ProcessErrorAndGetFailure(int expectedEventSequence, int latestEventSequence, DateTimeOffset timestamp)
    {
        var tags = new Dictionary<string, object?>
        {
            { "ExpectedEventSequence", expectedEventSequence },
            { "LatestEventSequence", latestEventSequence }
        };

        Activity.Current?.AddEvent(new ActivityEvent(
            name: "Concurrency exception",
            timestamp,
            tags: new ActivityTagsCollection(tags)));

        return new Failure
        (
            Title: "Error",
            Description: "There was an error when processing the request"
        );
    }
}
