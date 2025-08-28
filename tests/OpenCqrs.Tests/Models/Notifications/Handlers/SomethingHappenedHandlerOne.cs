using OpenCqrs.Notifications;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Notifications.Handlers;

public class SomethingHappenedHandlerOne : INotificationHandler<SomethingHappened>
{
    public Task<Result> Handle(SomethingHappened notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Handler One processed: {notification.Name}");

        return Task.FromResult(Result.Ok());
    }
}
