using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;
using OpenCqrs.Queries;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Queries;

public record GetOrderAggregateQuery(Guid CustomerId, Guid OrderId) : IQuery<Order?>;
