using OpenCqrs.Commands;

namespace OpenCqrs.Tests.Models.Commands;

public record DoMore(string Name) : ICommand<CommandResponse>;
