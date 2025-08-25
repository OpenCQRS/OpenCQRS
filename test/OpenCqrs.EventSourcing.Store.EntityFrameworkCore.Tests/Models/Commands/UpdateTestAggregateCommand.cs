using OpenCqrs.Commands;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Commands;

public record UpdateTestAggregateCommand(string Id, string Name, string Description) : ICommand;
