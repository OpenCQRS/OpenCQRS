using Microsoft.Azure.Cosmos;
using System.ComponentModel.DataAnnotations;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Configuration;

public class CosmosOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string AuthKey { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "OpenCqrs";

    public string ContainerName { get; set; } = "Domain";

    public CosmosClientOptions ClientOptions { get; set; } = new()
    {
        ApplicationName = "OpenCqrs",
        ConnectionMode = ConnectionMode.Direct
    };
}
