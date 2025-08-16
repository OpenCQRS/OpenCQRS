using OpenCqrs.Domain;

namespace OpenCqrs.Store.EF.Entities.Factories
{
    public interface IEventEntityFactory
    {
        EventEntity CreateEvent(IDomainEvent @event, int version);
    }
}
