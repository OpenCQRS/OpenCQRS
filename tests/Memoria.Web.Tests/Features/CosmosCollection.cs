using Xunit;

namespace Memoria.Web.Tests.Features;

/// <summary>
/// The emulator-backed reads, run one class at a time.
/// </summary>
/// <remarks>
/// Each of these classes creates a database of its own and deletes it afterwards, which is what
/// keeps one run's rows out of the next. Run in parallel, two of those against the emulator is
/// enough to make it answer <c>InternalServerError</c> to an ordinary query — the emulator is a
/// single process on this machine, not the service. They are collected here so xUnit runs them in
/// sequence, which costs a few seconds and removes a failure that says nothing about the code.
/// </remarks>
[CollectionDefinition(Name)]
public class CosmosCollection
{
    public const string Name = "Cosmos emulator";
}
