using OpenCqrs.Domain;

namespace OpenCqrs.Store.Cosmos.Sql.Documents.Factories
{
    public interface IEventDocumentFactory
    {
        EventDocument CreateEvent(IDomainEvent @event, int version);
    }
}
