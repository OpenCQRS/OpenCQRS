using System;
using OpenCqrs.Events;

namespace OpenCqrs.Domain
{
    public interface IDomainEvent : IEvent
    {
        Guid Id { get; set; }
        Guid AggregateRootId { get; set; }
        Guid CommandId { get; set; }
        void Update(IDomainCommand command);
    }
}
