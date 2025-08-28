using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Tests.Models.Streams;

public class TestStreamId(string id) : IStreamId
{
    public string Id => $"test:{id}";
}
