using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenCqrs.Extensions;
using OpenCqrs.Store.Cosmos.Sql.Configuration;

namespace OpenCqrs.Store.Cosmos.Sql.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IOpenCqrsAppBuilder EnsureCosmosDbSqlDbCreated(this IOpenCqrsAppBuilder builder, IOptions<CosmosDbOptions> settings)
        {
            var documentClient = builder.App.ApplicationServices.GetService<IDocumentClient>();

            CreateDatabaseIfNotExistsAsync(documentClient, settings.Value.DatabaseId).Wait();
            CreateCollectionIfNotExistsAsync(documentClient, settings.Value.AggregateCollectionId, settings).Wait();
            CreateCollectionIfNotExistsAsync(documentClient, settings.Value.CommandCollectionId, settings).Wait();
            CreateCollectionIfNotExistsAsync(documentClient, settings.Value.EventCollectionId, settings).Wait();

            return builder;
        }

        private static async Task CreateDatabaseIfNotExistsAsync(IDocumentClient documentClient, string databaseId)
        {
            try
            {
                await documentClient.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await documentClient.CreateDatabaseAsync(new Database 
                    { 
                        Id = databaseId 
                    });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync(IDocumentClient documentClient, string collectionId, IOptions<CosmosDbOptions> settings)
        {
            try
            {
                await documentClient.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(settings.Value.DatabaseId, collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await documentClient.CreateDocumentCollectionIfNotExistsAsync(
                        UriFactory.CreateDatabaseUri(settings.Value.DatabaseId),
                        new DocumentCollection 
                        { 
                            Id = collectionId, 
                            PartitionKey = new PartitionKeyDefinition 
                            {  
                                Paths = new Collection<string>
                                {
                                    "/type"
                                }
                            } 
                        },
                        new RequestOptions 
                        { 
                            OfferThroughput = settings.Value.OfferThroughput,
                            ConsistencyLevel = settings.Value.ConsistencyLevel
                        });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}