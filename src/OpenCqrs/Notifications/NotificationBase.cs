namespace OpenCqrs.Notifications;

public record NotificationBase : INotification
{
    public DateTimeOffset TimeStamp { get; init; } = DateTimeOffset.UtcNow;
}
