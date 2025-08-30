// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

using OpenCqrs.EventSourcing.Data;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

public class AggregateEventEntityForEfCore : AggregateEventEntity
{
    /// <summary>
    /// Gets or sets the navigation property to the associated aggregate entity.
    /// Enables Entity Framework to automatically load aggregate data when querying through this relationship.
    /// </summary>
    /// <value>
    /// An <see cref="AggregateEntity"/> instance representing the aggregate that owns the associated event.
    /// This property is virtual to support Entity Framework lazy loading and proxy generation.
    /// </value>
    public virtual AggregateEntity Aggregate { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the associated domain event entity.
    /// Enables Entity Framework to automatically load event data when querying through this relationship.
    /// </summary>
    /// <value>
    /// An <see cref="EventEntity"/> instance representing the domain event associated with the aggregate.
    /// This property is virtual to support Entity Framework lazy loading and proxy generation.
    /// </value>
    public virtual EventEntity Event { get; set; } = null!;
}
