using System.Collections.ObjectModel;

namespace OpenCqrs.Commands;

public interface ICommandSequence<TResponse>
{
    ReadOnlyCollection<ICommand<TResponse>> Commands { get; }
}
