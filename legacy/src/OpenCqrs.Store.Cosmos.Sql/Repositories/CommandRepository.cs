using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using OpenCqrs.Store.Cosmos.Sql.Configuration;
using OpenCqrs.Store.Cosmos.Sql.Documents;

namespace OpenCqrs.Store.Cosmos.Sql.Repositories
{
    public class CommandRepository : BaseDocumentRepository<CommandDocument>
    {
        public CommandRepository(IDocumentClient documentClient, IOptions<CosmosDbOptions> settings) 
            : base(settings.Value.CommandCollectionId, documentClient, settings)
        {
        }
    }
}