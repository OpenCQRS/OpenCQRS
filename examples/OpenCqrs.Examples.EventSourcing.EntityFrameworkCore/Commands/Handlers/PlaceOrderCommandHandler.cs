﻿using OpenCqrs.Commands;
using OpenCqrs.EventSourcing;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Aggregates;
using OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Streams;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Commands.Handlers;

public class PlaceOrderCommandHandler(IDomainService domainService) : ICommandHandler<PlaceOrderCommand>
{
    public async Task<Result> Handle(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var customerStreamId = new CustomerStreamId(command.CustomerId);
        var orderAggregateId = new OrderId(command.OrderId);

        var orderAggregate = new Order(command.OrderId, command.Amount);

        return await domainService.SaveAggregate(customerStreamId, orderAggregateId, orderAggregate, expectedEventSequence: 0, cancellationToken);
    }
}
