using OpenCqrs.Commands;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Extensions.DbContextExtensions;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Data;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Streams;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Commands.Handlers;

public class PlaceOrderCommandHandler(MyStoreDbContext dbContext) : ICommandHandler<PlaceOrderCommand>
{
    public async Task<Result> Handle(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(command.CustomerId);
        var orderAggregateId = new OrderAggregateId(command.OrderId);
        
        var orderAggregate = new OrderAggregate(command.OrderId, command.Amount);
        
        return await dbContext.SaveAggregate(customerStreamId, orderAggregateId, orderAggregate, expectedEventSequence: 0, cancellationToken);
    }
}
