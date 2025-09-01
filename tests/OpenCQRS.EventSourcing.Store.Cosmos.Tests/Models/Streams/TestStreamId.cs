using OpenCqrs.EventSourcing.Domain;

namespace OpenCQRS.EventSourcing.Store.Cosmos.Tests.Models.Streams;

public class TestStreamId(string id) : IStreamId
{
    public string Id => $"test:{id}";
}
