using OpenCqrs.Examples.EventSourcing.Cosmos.Aggregates;
using OpenCqrs.Queries;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.Queries;

public record GetOrderAggregateQuery(Guid CustomerId, Guid OrderId) : IQuery<OrderAggregateRoot>;
