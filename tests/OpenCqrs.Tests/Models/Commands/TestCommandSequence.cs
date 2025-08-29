using OpenCqrs.Commands;

namespace OpenCqrs.Tests.Models.Commands;

public class TestCommandSequence : CommandSequence
{
    public TestCommandSequence()
    {
        AddCommand(new FirstCommandInSequence(Name: "Test Name" ));
        AddCommand(new SecondCommandInSequence());
        AddCommand(new ThirdCommandInSequence());
    }
}
