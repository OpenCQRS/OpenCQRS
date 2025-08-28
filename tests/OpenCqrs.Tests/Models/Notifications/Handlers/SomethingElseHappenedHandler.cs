using OpenCqrs.Notifications;
using OpenCqrs.Results;

namespace OpenCqrs.Tests.Models.Notifications.Handlers;

public class SomethingElseHappenedHandler : INotificationHandler<SomethingElseHappened>
{
    public Task<Result> Handle(SomethingElseHappened notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Handler processed: {notification.Name}");

        return Task.FromResult(Result.Fail());
    }
}
