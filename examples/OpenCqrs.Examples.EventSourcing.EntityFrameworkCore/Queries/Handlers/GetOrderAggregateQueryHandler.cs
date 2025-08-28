using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Data;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Streams;
using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Queries.Handlers;

public class GetOrderAggregateQueryHandler(MyStoreDbContext dbContext) : IQueryHandler<GetOrderAggregateQuery, OrderAggregate>
{
    public async Task<Result<OrderAggregate>> Handle(GetOrderAggregateQuery query, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(query.CustomerId);
        var orderAggregateId = new OrderAggregateId(query.OrderId);

        return await dbContext.GetAggregate(customerStreamId, orderAggregateId, applyNewDomainEvents: false, cancellationToken);
    }
}
