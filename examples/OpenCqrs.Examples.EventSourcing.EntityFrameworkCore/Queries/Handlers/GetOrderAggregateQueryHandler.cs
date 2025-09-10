using OpenCqrs.EventSourcing;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Streams;
using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Queries.Handlers;

public class GetOrderAggregateQueryHandler(IDomainService domainService) : IQueryHandler<GetOrderAggregateQuery, OrderAggregateRoot>
{
    public async Task<Result<OrderAggregateRoot>> Handle(GetOrderAggregateQuery query, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(query.CustomerId);
        var orderAggregateId = new OrderAggregateId(query.OrderId);

        return await domainService.GetAggregate(customerStreamId, orderAggregateId, applyNewDomainEvents: false, cancellationToken);
    }
}
