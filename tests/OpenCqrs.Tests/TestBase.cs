using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenCqrs.Commands;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Tests.Models.Commands;
using OpenCqrs.Tests.Models.Commands.Handlers;
using OpenCqrs.Tests.Models.Commands.Validators;
using OpenCqrs.Tests.Models.Notifications;
using OpenCqrs.Tests.Models.Notifications.Handlers;

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
            .AddSingleton<IValidator<DoSomething>, DoSomethingValidator>()
            .BuildServiceProvider();

        var publisher = new Publisher(serviceProvider);
        var commandSender = new CommandSender(serviceProvider, publisher);
        
        Dispatcher = new Dispatcher(commandSender, Substitute.For<IQueryProcessor>(), publisher);
    }
}
