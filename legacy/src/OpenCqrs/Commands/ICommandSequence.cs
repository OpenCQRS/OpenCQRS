using System.Collections.ObjectModel;

namespace OpenCqrs.Commands
{
    public interface ICommandSequence
    {
        ReadOnlyCollection<ICommand> Commands { get; }
    }
}
