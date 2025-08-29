using System.Collections.ObjectModel;

namespace OpenCqrs.Commands;

public abstract class CommandSequence<TResponse> : ICommandSequence<TResponse>
{
    private readonly List<ICommand<TResponse>> _commands = [];
    public ReadOnlyCollection<ICommand<TResponse>> Commands => _commands.AsReadOnly();

    /// <summary>
    /// Adds the command to the sequence collection.
    /// </summary>
    /// <param name="command">The command.</param>
    public void AddCommand(ICommand<TResponse> command)
    {
        _commands.Add(command);
    }
}
