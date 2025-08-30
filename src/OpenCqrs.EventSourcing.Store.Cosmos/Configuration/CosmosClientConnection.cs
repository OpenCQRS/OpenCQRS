using Microsoft.Azure.Cosmos;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

public record CosmosClientConnection(
    string Endpoint,
    string AuthKey,
    string DatabaseName,
    string ContainerName,
    CosmosClientOptions? ClientOptions = null) : ICosmosClientConnection;
