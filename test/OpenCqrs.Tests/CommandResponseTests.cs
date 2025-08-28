using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenCqrs.Commands;
using OpenCqrs.Notifications;
using OpenCqrs.Queries;
using OpenCqrs.Tests.Models.Commands;
using OpenCqrs.Tests.Models.Commands.Handlers;
using OpenCqrs.Tests.Models.Notifications;
using OpenCqrs.Tests.Models.Notifications.Handlers;
using Xunit;

namespace OpenCqrs.Tests;

public class CommandResponseTests
{
    private readonly IDispatcher _dispatcher;
    
    public CommandResponseTests()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ICommandHandler<DoSomething, CommandResponse>, DoSomethingHandler>()
            .AddSingleton<ICommandHandler<DoMore, CommandResponse>, DoMoreHandler>()
            .AddSingleton<INotificationHandler<SomethingHappened>, SomethingHappenedHandlerOne>()
            .AddSingleton<INotificationHandler<SomethingHappened>, SomethingHappenedHandlerTwo>()
            .AddSingleton<INotificationHandler<SomethingElseHappened>, SomethingElseHappenedHandler>()
            .BuildServiceProvider();

        var publisher = new Publisher(serviceProvider);
        var commandSender = new CommandSender(serviceProvider, publisher);
        
        _dispatcher = new Dispatcher(commandSender, Substitute.For<IQueryProcessor>(), publisher);
    }
    
    [Fact]
    public async Task SendAndPublish_Should_Call_NotificationHandlers_For_CommandResponse_With_SingleNotification()
    {
        var result = await _dispatcher.SendAndPublish(new DoSomething("TestName"));

        using (new AssertionScope())
        {
            result.CommandResult.Should().NotBeNull();
            result.CommandResult.Value.Should().NotBeNull();
            result.CommandResult.Value.Notifications.Should().NotBeNull();
            result.CommandResult.Value.Notifications.Count().Should().Be(1);
            
            result.NotificationResults.Should().NotBeNull();
            result.NotificationResults.Count().Should().Be(2);
            result.NotificationResults.All(r => r.IsSuccess).Should().BeTrue();
        }
    }
    
    [Fact]
    public async Task SendAndPublish_Should_Call_NotificationHandlers_For_CommandResponse_With_MultipleNotifications_And_MultipleResults()
    {
        var result = await _dispatcher.SendAndPublish(new DoMore("TestName"));

        using (new AssertionScope())
        {
            result.CommandResult.Should().NotBeNull();
            result.CommandResult.Value.Should().NotBeNull();
            result.CommandResult.Value.Notifications.Should().NotBeNull();
            result.CommandResult.Value.Notifications.Count().Should().Be(2);
            
            result.NotificationResults.Should().NotBeNull();
            result.NotificationResults.Count().Should().Be(3);
            result.NotificationResults.Count(r => r.IsSuccess).Should().Be(2);
            result.NotificationResults.Count(r => r.IsNotSuccess).Should().Be(1);
        }
    }
}
