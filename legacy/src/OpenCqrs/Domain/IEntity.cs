using System;

namespace OpenCqrs.Domain
{
    public interface IEntity
    {
        Guid Id { get; }
    }
}
