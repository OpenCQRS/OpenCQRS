using OpenCqrs.Results;

namespace OpenCqrs.Notifications;

public interface IPublisher
{
    Task<IEnumerable<Result>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}
