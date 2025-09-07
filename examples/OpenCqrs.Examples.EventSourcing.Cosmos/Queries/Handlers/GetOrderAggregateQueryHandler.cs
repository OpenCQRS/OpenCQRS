using OpenCqrs.EventSourcing;
using OpenCqrs.Examples.EventSourcing.Cosmos.Aggregates;
using OpenCqrs.Examples.EventSourcing.Cosmos.Streams;
using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.Queries.Handlers;

public class GetOrderAggregateQueryHandler(IDomainService domainService) : IQueryHandler<GetOrderAggregateQuery, OrderAggregate>
{
    public async Task<Result<OrderAggregate>> Handle(GetOrderAggregateQuery query, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(query.CustomerId);
        var orderAggregateId = new OrderAggregateId(query.OrderId);

        return await domainService.GetAggregate(customerStreamId, orderAggregateId, applyNewDomainEvents: false, cancellationToken);
    }
}
