using OpenCqrs.Commands;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Commands;

public record UpdateTestAggregateCommand(string Id, string Name, string Description) : ICommand;
