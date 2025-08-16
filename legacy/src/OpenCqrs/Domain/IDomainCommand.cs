using System;
using OpenCqrs.Commands;

namespace OpenCqrs.Domain
{
    public interface IDomainCommand : ICommand
    {
        Guid Id { get; set; }
        Guid AggregateRootId { get; set; }
        int? ExpectedVersion { get; set; }
        bool? SaveCommandData { get; set; }
    }

    public interface IDomainCommand<out TAggregateRoot> : IDomainCommand 
        where TAggregateRoot : IAggregateRoot
    {
    }
}
