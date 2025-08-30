using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public class EventDocument : DocumentBase, IAuditableDocument, IBindableDocument
{
    public string Id { get; set; } = null!;

    public int Sequence { get; set; }

    public string Data { get; set; } = null!;

    public DateTimeOffset CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public string TypeName { get; set; } = null!;

    public int TypeVersion { get; set; }
}

public static class EventDocumentExtensions
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver()
    };

    public static IDomainEvent ToDomainEvent(this EventDocument eventDocument)
    {
        var typeFound = TypeBindings.DomainEventTypeBindings.TryGetValue(eventDocument.GetTypeBindingKey(), out var eventType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Event type {eventDocument.TypeName} not found in TypeBindings");
        }

        return (IDomainEvent)JsonConvert.DeserializeObject(eventDocument.Data, eventType!, JsonSerializerSettings)!;
    }
}
