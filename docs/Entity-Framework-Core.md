# Entity Framework Core

The OpenCQRS Entity Framework Core store provider enables event sourcing persistence using Entity Framework Core. This provider offers a comprehensive set of extension methods for managing aggregates and events.

All features are implemented as extension methods on the `IDomainDbContext` interface, allowing seamless integration with your existing DbContext implementations.

It also means that you can use the OpenCQRS mediator pattern, any other mediator library, or classic service classes without any dependency on a specific mediator.

The event sourcing functionalities can used with the following Entity Framework Core database providers:
- SQL Server
- SQLite
- PostgreSQL
- MySQL
- In-Memory

OpenCQRS also provides support for IdentityDbContext from ASP.NET Core Identity, allowing you to integrate event sourcing with user management and authentication features.

## Usage

_Work in progress_

## Extension Methods

The Entity Framework Core store provider offers a variety of built-in extension methods to facilitate interaction with aggregates and events. Since the store provider is based purely on the DbContext, it's extremily easy to create your own extensions to create any kind of reporting. Below is a categorized list of the built-in methods:

### Saving

| Method               | Description                                                                                                                                                                                                                                                             |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **SaveAggregate**    | Saves an aggregate to the event store with optimistic concurrency control, persisting all uncommitted domain events and updating the aggregate snapshot.                                                                                                                |
| **SaveDomainEvents** | Saves an array of domain events to the event store with optimistic concurrency control, bypassing aggregate persistence. This method is ideal for scenarios where events are generated outside traditional aggregate workflows.                                         |
| **Save**             | Saves all pending changes in the domain database context to the underlying data store. This method provides a simple way to persist tracked entity changes without additional event sourcing logic, suitable for scenarios where entities have been explicitly tracked. |
| **UpdateAggregate**  | Updates an existing aggregate with new events from its stream, applying any events that occurred after the aggregate's last known state.                                                                                                                                |

### Tracking

| Method                     | Description                                                                                                                                                                                                               |
|----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **TrackWithAggregate**     | Tracks an aggregate's uncommitted events and state changes in the Entity Framework change tracker without persisting to the database, preparing all necessary entities for subsequent save operations.                    |
| **TrackWithDomainEvents**  | Tracks an array of domain events in the Entity Framework change tracker without persisting to the database, preparing event entities for later save operations with proper sequencing and concurrency control validation. |
| **TrackWithEventEntities** | Tracks an aggregate's state changes based on a list of event entities, applying only events that the aggregate can handle and updating its snapshot accordingly.                                                          |

### Retrieving Aggregates and Domain Events

| Method                                | Description                                                                                                                                                                                                                                                                                    |
|---------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **GetAggregate**                      | Retrieves an aggregate from the event store, either from its snapshot or by reconstructing it from events.                                                                                                                                                                                     |
| **GetInMemoryAggregate**              | Reconstructs an aggregate entirely from events without using snapshots, providing a pure event-sourced view of the aggregate state.                                                                                                                                                            |
| **GetDomainEvents**                   | Retrieves all domain events from a specified stream, with optional filtering by event types.                                                                                                                                                                                                   |
| **GetDomainEventsFromSequence**       | Retrieves domain events from a specified stream starting from a specific sequence number onwards, with optional filtering by event types.                                                                                                                                                      |
| **GetDomainEventsUpToSequence**       | Retrieves domain events from a specified stream up to and including a specific sequence number, with optional filtering by event types.                                                                                                                                                        |
| **GetDomainEventsAppliedToAggregate** | Retrieves all domain events that have been applied to a specific aggregate instance, using the explicit aggregate-event relationship tracking. This method provides precise access to the events that actually contributed to an aggregate's current state.                                    |
| **GetLatestEventSequence**            | Retrieves the latest event sequence number for a specified stream, with optional filtering by event types. This method provides the current position in an event stream, essential for optimistic concurrency control and determining where to append new events in event sourcing operations. |

### Retrieving Database Entities

| Method                                 | Description                                                                                                                                                                                                        |
|----------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **GetEventEntities**                   | Retrieves all event entities from a specified stream, with optional filtering by event types.                                                                                                                      |
| **GetEventEntitiesFromSequence**       | Retrieves a list of event entities from the specified stream starting from a given sequence number, with optional filtering by event types.                                                                        |
| **GetEventEntitiesUpToSequence**       | Retrieves event entities from a specified stream up to and including a specific sequence number, with optional filtering by event types.                                                                           |
| **GetEventEntitiesAppliedToAggregate** | Retrieves all event entities that have been applied to a specific aggregate instance, providing a complete audit trail of changes that contributed to the aggregate's current state.                               |
| **GetAggregateEventEntities**          | Retrieves all aggregate-event relationship entities associated with a specific aggregate instance, providing complete visibility into the many-to-many relationships between the aggregate and its applied events. |
