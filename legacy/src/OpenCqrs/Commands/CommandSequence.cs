using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenCqrs.Commands
{
    public abstract class CommandSequence : ICommandSequence
    {
        private readonly List<ICommand> _commands = new List<ICommand>();
        public ReadOnlyCollection<ICommand> Commands => _commands.AsReadOnly();

        /// <summary>
        /// Adds the command to the sequence collection.
        /// </summary>
        /// <param name="command">The command.</param>
        protected void AddCommand(ICommand command)
        {
            _commands.Add(command);
        }
    }
}
