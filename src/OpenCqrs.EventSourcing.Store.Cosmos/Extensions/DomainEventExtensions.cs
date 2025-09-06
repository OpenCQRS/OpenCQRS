using System.Reflection;
using Newtonsoft.Json;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.Cosmos.Documents;

namespace OpenCqrs.EventSourcing.Store.Cosmos.Extensions;

/// <summary>
/// Provides extension methods for converting domain events to documents suitable for storage in Cosmos DB.
/// These methods facilitate the transformation between domain events and their document representations.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Converts a domain event to an event document for storage in Cosmos DB.
    /// This method extracts event metadata, generates a unique identifier, and serializes the event data.
    /// </summary>
    /// <param name="domainEvent">The domain event to convert to a document.</param>
    /// <param name="streamId">The stream identifier associated with the event.</param>
    /// <param name="sequence">The sequence number of the event within the stream.</param>
    /// <returns>An <see cref="EventDocument"/> containing the serialized event data and metadata.</returns>
    /// <exception cref="Exception">Thrown when the domain event type does not have a DomainEventType attribute.</exception>
    public static EventDocument ToEventDocument(this IDomainEvent domainEvent, IStreamId streamId, int sequence)
    {
        var domainEventType = domainEvent.GetType().GetCustomAttribute<DomainEventType>();
        if (domainEventType == null)
        {
            throw new Exception($"Domain event {domainEvent.GetType().Name} does not have a DomainEventType attribute.");
        }

        return new EventDocument
        {
            Id = $"{streamId.Id}:{sequence}",
            StreamId = streamId.Id,
            Sequence = sequence,
            EventType = TypeBindings.GetTypeBindingKey(domainEventType.Name, domainEventType.Version),
            Data = JsonConvert.SerializeObject(domainEvent)
        };
    }
}
