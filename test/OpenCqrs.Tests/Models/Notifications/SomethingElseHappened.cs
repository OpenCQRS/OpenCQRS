using OpenCqrs.Notifications;

namespace OpenCqrs.Tests.Models.Notifications;

public record SomethingElseHappened(string Name) : INotification;
