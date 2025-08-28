using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenCqrs.Commands;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Tests.Models.Commands;
using OpenCqrs.Tests.Models.Commands.Handlers;
using OpenCqrs.Tests.Models.Notifications;
using OpenCqrs.Tests.Models.Notifications.Handlers;
using OpenCqrs.Validation;

namespace OpenCqrs.Tests;

public abstract class TestBase
{
    protected readonly IDispatcher Dispatcher;

    protected TestBase()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ICommandHandler<DoSomething, CommandResponse>, DoSomethingHandler>()
            .AddSingleton<ICommandHandler<DoMore, CommandResponse>, DoMoreHandler>()
            .AddSingleton<INotificationHandler<SomethingHappened>, SomethingHappenedHandlerOne>()
            .AddSingleton<INotificationHandler<SomethingHappened>, SomethingHappenedHandlerTwo>()
            .AddSingleton<INotificationHandler<SomethingElseHappened>, SomethingElseHappenedHandler>()
            .BuildServiceProvider();

        var publisher = new Publisher(serviceProvider);
        var commandSender = new CommandSender(serviceProvider, Substitute.For<IValidationService>(), publisher);

        Dispatcher = new Dispatcher(commandSender, Substitute.For<IQueryProcessor>(), publisher);
    }
}
