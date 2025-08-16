using System.Threading.Tasks;

namespace OpenCqrs.Bus
{
    public interface IBusMessageDispatcher
    {
        Task DispatchAsync<TMessage>(TMessage message) where TMessage : IBusMessage;
    }
}
