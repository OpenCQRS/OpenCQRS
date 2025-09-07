using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DataStore;

/// <summary>
/// Provides data access operations for the Cosmos DB Event Sourcing store.
/// This class handles the storage and retrieval of aggregates, events, and aggregate event documents in Cosmos DB.
/// </summary>
public partial class CosmosDataStore : ICosmosDataStore
{
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDataStore"/> class.
    /// </summary>
    /// <param name="options">The Cosmos DB configuration options.</param>
    /// <param name="timeProvider">The time provider for timestamp operations.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for retrieving user information.</param>
    public CosmosDataStore(IOptions<CosmosOptions> options, TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor)
    {
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _cosmosClient = new CosmosClient(options.Value.Endpoint, options.Value.AuthKey, options.Value.ClientOptions);
        _container = _cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the CosmosDataStore and optionally releases the managed resources.
    /// This method disposes of the Cosmos client connection.
    /// </summary>
    public void Dispose()
    {
        _cosmosClient.Dispose();
    }
}
