using System;
using OpenCqrs.Events;

namespace OpenCqrs.Domain
{
    public abstract class DomainEvent : Event, IDomainEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AggregateRootId { get; set; }
        public Guid CommandId { get; set; }
        public void Update(IDomainCommand command)
        {
            CommandId = command.Id;
            UserId = command.UserId;
            Source = command.Source;
        }
    }
}
