using System.Threading.Tasks;

namespace OpenCqrs.Commands
{
    public interface ICommandHandlerAsync<in TCommand> where TCommand : ICommand
    {
        Task<CommandResponse> HandleAsync(TCommand command);
    }
}
