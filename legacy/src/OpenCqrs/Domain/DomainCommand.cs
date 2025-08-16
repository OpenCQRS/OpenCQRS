using System;
using OpenCqrs.Commands;

namespace OpenCqrs.Domain
{
    public abstract class DomainCommand<TAggregateRoot> : Command, IDomainCommand<TAggregateRoot> 
        where TAggregateRoot : IAggregateRoot
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AggregateRootId { get; set; } = Guid.NewGuid();
        public int? ExpectedVersion { get; set; }
        public bool? SaveCommandData { get; set; }
    }
}
