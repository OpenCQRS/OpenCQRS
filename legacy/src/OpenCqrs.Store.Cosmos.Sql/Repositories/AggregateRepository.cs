using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using OpenCqrs.Store.Cosmos.Sql.Configuration;
using OpenCqrs.Store.Cosmos.Sql.Documents;

namespace OpenCqrs.Store.Cosmos.Sql.Repositories
{
    public class AggregateRepository : BaseDocumentRepository<AggregateDocument>
    {
        public AggregateRepository(IDocumentClient documentClient, IOptions<CosmosDbOptions> settings) 
            : base(settings.Value.AggregateCollectionId, documentClient, settings)
        {
        }
    }
}