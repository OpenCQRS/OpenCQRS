using Microsoft.Azure.Cosmos;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

public interface ICosmosClientConnection
{
    string Endpoint { get; init; }
    string AuthKey { get; init; }
    string DatabaseName { get; init; }
    CosmosClientOptions? ClientOptions { get; init; }
}
