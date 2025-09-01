using OpenCqrs.Commands;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Commands;

public record CreateTestAggregateCommand(string Id, string Name, string Description) : ICommand;
