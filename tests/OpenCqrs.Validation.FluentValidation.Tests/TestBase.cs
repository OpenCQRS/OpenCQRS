using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenCqrs.Commands;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Validation.FluentValidation.Tests.Models.Commands;
using OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Handlers;
using OpenCqrs.Validation.FluentValidation.Tests.Models.Commands.Validators;

namespace OpenCqrs.Validation.FluentValidation.Tests;

public abstract class TestBase
{
    protected readonly IDispatcher Dispatcher;

    protected TestBase()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ICommandHandler<DoSomething>, DoSomethingHandler>()
            .AddSingleton<IValidator<DoSomething>, DoSomethingValidator>()
            .BuildServiceProvider();

        var publisher = new Publisher(serviceProvider);
        var commandSender = new CommandSender(serviceProvider, publisher);
        
        Dispatcher = new Dispatcher(commandSender, Substitute.For<IQueryProcessor>(), publisher);
    }
}
