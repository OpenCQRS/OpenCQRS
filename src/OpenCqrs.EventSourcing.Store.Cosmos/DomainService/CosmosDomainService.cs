using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.DomainService;
using OpenCqrs.EventSourcing.Store.Cosmos.Configuration;
using OpenCqrs.Results;

namespace OpenCqrs.EventSourcing.Store.Cosmos.DomainService;

/// <summary>
/// Cosmos DB implementation of the domain service for event sourcing operations.
/// </summary>
public partial class CosmosDomainService : IDomainService
{
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ICosmosDataStore _cosmosDataStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDomainService"/> class.
    /// </summary>
    /// <param name="options">Cosmos DB configuration options.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for user information.</param>
    /// <param name="cosmosDataStore">The Cosmos data store for document operations.</param>
    public CosmosDomainService(IOptions<CosmosOptions> options, TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor, ICosmosDataStore cosmosDataStore)
    {
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _cosmosClient = new CosmosClient(options.Value.Endpoint, options.Value.AuthKey, options.Value.ClientOptions);
        _container = _cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);
        _cosmosDataStore = cosmosDataStore;
    }

    /// <summary>
    /// Disposes the Cosmos client resources.
    /// </summary>
    public void Dispose() => _cosmosClient.Dispose();
}
