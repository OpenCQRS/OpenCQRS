using OpenCqrs.Notifications;

namespace OpenCqrs.Commands;

public class CommandResponse
{
    public IEnumerable<INotification>? Notifications { get; set; }
    public object? Result { get; set; }

    public CommandResponse()
    {
    }

    public CommandResponse(INotification notification, object? result = null)
    {
        Notifications = new List<INotification>
        {
            notification
        };

        Result = result;
    }

    public CommandResponse(IEnumerable<INotification> notifications, object? result = null)
    {
        Notifications = notifications;
        Result = result;
    }
}
