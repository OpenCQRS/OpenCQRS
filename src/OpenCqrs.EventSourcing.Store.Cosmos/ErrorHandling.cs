using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos;

public static class ErrorHandling
{
    public static Failure DefaultFailure => new(
        Title: "Error",
        Description: "There was an error when processing the request"
    );
}
