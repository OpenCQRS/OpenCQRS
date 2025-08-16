namespace OpenCqrs.Notifications;

public interface INotification
{
    DateTimeOffset TimeStamp { get; init; }
}
