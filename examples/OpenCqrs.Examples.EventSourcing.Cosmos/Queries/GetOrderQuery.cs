using OpenCqrs.Examples.EventSourcing.Cosmos.Aggregates;
using OpenCqrs.Queries;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.Queries;

public record GetOrderQuery(Guid CustomerId, Guid OrderId) : IQuery<Order>;
