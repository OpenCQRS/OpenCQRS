using OpenCqrs.Results;

namespace OpenCqrs.Notifications;

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task<Result> Handle(TNotification notification, CancellationToken cancellationToken = default);
}
