using OpenCqrs.EventSourcing.Domain;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Documents;

public interface IBindableDocument
{
    string TypeName { get; set; }

    int TypeVersion { get; set; }
}

public static class BindableDocumentExtensions
{
    public static string GetTypeBindingKey(this IBindableDocument bindableDocument) =>
        TypeBindings.GetTypeBindingKey(bindableDocument.TypeName, bindableDocument.TypeVersion);

    public static Type ToDomainEventType(this IBindableDocument bindableDocument)
    {
        var typeFound = TypeBindings.DomainEventTypeBindings.TryGetValue(bindableDocument.GetTypeBindingKey(), out var eventType);
        if (typeFound is false)
        {
            throw new InvalidOperationException($"Event type {bindableDocument.TypeName} not found in TypeBindings");
        }

        return eventType!;
    }
}
