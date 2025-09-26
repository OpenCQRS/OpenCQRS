using OpenCqrs.EventSourcing;
using OpenCqrs.Examples.EventSourcing.Cosmos.Aggregates;
using OpenCqrs.Examples.EventSourcing.Cosmos.Streams;
using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.Queries.Handlers;

public class GetOrderAggregateQueryHandler(IDomainService domainService) : IQueryHandler<GetOrderQuery, Order?>
{
    public async Task<Result<Order?>> Handle(GetOrderQuery query, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(query.CustomerId);
        var orderAggregateId = new OrderId(query.OrderId);

        return await domainService.GetAggregate(customerStreamId, orderAggregateId, ReadMode.LatestSnapshot, cancellationToken);
    }
}
