using OpenCqrs.Domain;

namespace OpenCqrs.Store.Cosmos.Sql.Documents.Factories
{
    public interface ICommandDocumentFactory
    {
        CommandDocument CreateCommand(IDomainCommand command);
    }
}
