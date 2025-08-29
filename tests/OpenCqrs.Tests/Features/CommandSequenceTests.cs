using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.Tests.Models.Commands;
using Xunit;

namespace OpenCqrs.Tests.Features;

public class CommandSequenceTests : TestBase
{
    [Fact]
    public async Task SendAndPublish_Should_Call_NotificationHandlers_For_CommandResponse_With_SingleNotification()
    {
        var sendResult = await Dispatcher.Send(new TestCommandSequence());
        var results = sendResult.ToList();
        
        using (new AssertionScope())
        {
            results.Count.Should().Be(3);
            results[0].IsSuccess.Should().BeTrue();
            results[1].IsSuccess.Should().BeFalse();
            results[2].IsSuccess.Should().BeTrue();
        }
    }
    
    // TODO: Stop processing on first failure
    
    // TODO: Commands with response?
}
