using Microsoft.Extensions.DependencyInjection;
using OpenCqrs.Results;

namespace OpenCqrs.Notifications;

public class Publisher(IServiceProvider serviceProvider) : IPublisher
{
    public async Task<IEnumerable<Result>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        var notificationHandlers = handlers as INotificationHandler<TNotification>[] ?? handlers.ToArray();
        if (notificationHandlers.Length == 0)
        {
            return [];
        }

        var tasks = notificationHandlers.Select(handler => handler.Handle(notification, cancellationToken)).ToList();

        return await Task.WhenAll(tasks);
    }
}
