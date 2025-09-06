using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenCqrs.Commands;
using OpenCqrs.Messaging.RabbitMq.Tests.Models.Commands;
using OpenCqrs.Messaging.RabbitMq.Tests.Models.Commands.Handlers;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Validation;

namespace OpenCqrs.Messaging.RabbitMq.Tests;

public abstract class TestBase
{
    protected readonly MockRabbitMqTestHelper MockRabbitMqTestHelper;
    protected readonly IDispatcher Dispatcher;

    protected TestBase()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ICommandHandler<DoSomething, CommandResponse>, DoSomethingHandler>()
            .BuildServiceProvider();

        MockRabbitMqTestHelper = new MockRabbitMqTestHelper();
        var serviceBusMessagingProvider = new RabbitMqMessagingProvider(MockRabbitMqTestHelper.MockOptions, MockRabbitMqTestHelper.MockConnection);
        var messagePublisher = new MessagePublisher(serviceBusMessagingProvider);
        var commandSender = new CommandSender(serviceProvider, Substitute.For<IValidationService>(), Substitute.For<INotificationPublisher>(), messagePublisher);

        Dispatcher = new Dispatcher(commandSender, Substitute.For<IQueryProcessor>(), Substitute.For<INotificationPublisher>());
    }
}
