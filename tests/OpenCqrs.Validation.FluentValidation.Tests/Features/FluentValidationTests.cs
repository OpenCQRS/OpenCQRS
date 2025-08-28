using FluentAssertions;
using FluentAssertions.Execution;
using OpenCqrs.Validation.FluentValidation.Tests.Models.Commands;
using Xunit;

namespace OpenCqrs.Validation.FluentValidation.Tests.Features;

public class FluentValidationTests : TestBase
{
    [Fact]
    public async Task Send_Should_Validate_The_Command_And_Return_Failure_If_Command_Is_Not_Valid()
    {
        var result = await Dispatcher.Send(command: new DoSomething(Name: string.Empty), validateCommand: true);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeFalse();
            result.Failure.Should().NotBeNull();
            result.Failure.Title.Should().Be("Validation Failed");
            result.Failure.Description.Should().Be("Validation failed with errors: Name is required.");
        }
    }

    [Fact]
    public async Task Send_Should_Validate_The_Command_And_Return_Success_If_Command_Is_Valid()
    {
        var result = await Dispatcher.Send(command: new DoSomething(Name: "Test Name"), validateCommand: true);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Failure.Should().BeNull();
        }
    }

    [Fact]
    public async Task SendWithResponse_Should_Validate_The_Command_And_Return_Failure_If_Command_Is_Not_Valid()
    {
        var result = await Dispatcher.Send(command: new DoSomethingWithResponse(Name: string.Empty), validateCommand: true);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeFalse();
            result.Failure.Should().NotBeNull();
            result.Failure.Title.Should().Be("Validation Failed");
            result.Failure.Description.Should().Be("Validation failed with errors: Name is required.");
        }
    }

    [Fact]
    public async Task SendWithResponse_Should_Validate_The_Command_And_Return_Success_If_Command_Is_Valid()
    {
        var result = await Dispatcher.Send(command: new DoSomethingWithResponse(Name: "Test Name"), validateCommand: true);

        using (new AssertionScope())
        {
            result.IsSuccess.Should().BeTrue();
            result.Failure.Should().BeNull();
        }
    }
    
    // TODO: Send and publish - failure
    
    // TODO: Send and publish - ok
}
