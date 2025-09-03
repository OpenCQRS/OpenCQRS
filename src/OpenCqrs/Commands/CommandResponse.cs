using OpenCqrs.Messaging;
using OpenCqrs.Notifications;

namespace OpenCqrs.Commands;

public class CommandResponse
{
    public IEnumerable<INotification> Notifications { get; set; } = [];
    public IEnumerable<IMessage> Messages { get; set; } = [];
    public object? Result { get; set; }

    public CommandResponse()
    {
    }

    public CommandResponse(object? result)
    {
        Result = result;
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

    public CommandResponse(IMessage message, object? result = null)
    {
        Messages = new List<IMessage>
        {
            message
        };

        Result = result;
    }

    public CommandResponse(IEnumerable<IMessage> messages, object? result = null)
    {
        Messages = messages;
        Result = result;
    }

    public CommandResponse(INotification notification, IMessage message, object? result = null)
    {
        Notifications = new List<INotification>
        {
            notification
        };

        Messages = new List<IMessage>
        {
            message
        };

        Result = result;
    }

    public CommandResponse(IEnumerable<INotification> notifications, IEnumerable<IMessage> messages, object? result = null)
    {
        Notifications = notifications;
        Messages = messages;
        Result = result;
    }

    public CommandResponse(IEnumerable<INotification> notifications, IMessage message, object? result = null)
    {
        Notifications = notifications;

        Messages = new List<IMessage>
        {
            message
        };

        Result = result;
    }

    public CommandResponse(INotification notification, IEnumerable<IMessage> messages, object? result = null)
    {
        Notifications = new List<INotification>
        {
            notification
        };

        Messages = messages;

        Result = result;
    }
}
