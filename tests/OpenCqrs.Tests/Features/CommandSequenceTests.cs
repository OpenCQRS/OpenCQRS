using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.Tests.Models.Commands;
using Xunit;

namespace OpenCqrs.Tests.Features;

public class CommandSequenceTests : TestBase
{
    [Fact]
    public async Task Send_Command_Sequence_Processes_All_Commands()
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
    
    [Fact]
    public async Task Send_Command_Sequence_Stops_Processing_On_First_Failure()
    {
        var sendResult = await Dispatcher.Send(new TestCommandSequence(), stopProcessingOnFirstFailure: true);
        var results = sendResult.ToList();
        
        using (new AssertionScope())
        {
            results.Count.Should().Be(2);
            results[0].IsSuccess.Should().BeTrue();
            results[1].IsSuccess.Should().BeFalse();
        }
    }
    
    // TODO: Commands with response?
}
