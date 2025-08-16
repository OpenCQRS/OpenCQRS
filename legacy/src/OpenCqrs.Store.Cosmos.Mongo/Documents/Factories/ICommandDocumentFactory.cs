using OpenCqrs.Domain;

namespace OpenCqrs.Store.Cosmos.Mongo.Documents.Factories
{
    public interface ICommandDocumentFactory
    {
        CommandDocument CreateCommand(IDomainCommand command);
    }
}
