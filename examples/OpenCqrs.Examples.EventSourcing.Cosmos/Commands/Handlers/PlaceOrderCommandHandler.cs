using OpenCqrs.Commands;
using OpenCqrs.EventSourcing;
using OpenCqrs.EventSourcing.DomainService;
using OpenCqrs.Examples.EventSourcing.Cosmos.Aggregates;
using OpenCqrs.Examples.EventSourcing.Cosmos.Streams;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.Cosmos.Commands.Handlers;

public class PlaceOrderCommandHandler(IDomainService domainService) : ICommandHandler<PlaceOrderCommand>
{
    public async Task<Result> Handle(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(command.CustomerId);
        var orderAggregateId = new OrderAggregateId(command.OrderId);

        var orderAggregate = new OrderAggregate(command.OrderId, command.Amount);

        return await domainService.SaveAggregate(customerStreamId, orderAggregateId, orderAggregate, expectedEventSequence: 0, cancellationToken);
    }
}
