# Release Notes

## OpenCQRS 7.1.0
_**Released ??/09/2025**_
- New methods in the domain service and DbContext(EntityFrameworkCore)/DataStore(CosmosDB):
  - Get domain events between two sequences

## OpenCQRS 7.0.0
_**Released 07/09/2025**_
- Upgrade to .NET 9
- New mediator pattern with commands, queries, and notifications
- Cosmos DB store provider
- Entity Framework Core store provider
- Extensions for db context in the Entity Framework Core store provider
- Support for IdentityDbContext from ASP.NET Core Identity
- Command validation
- Command sequences
- Automatic publishing of notifications and messages (ServiceBus or RabbitMQ) on the back of a successfully processed command
- Automatic caching of query results (MemoryCache or RedisCache)
- More flexible and extensible architecture

## OpenCQRS 7.0.0-rc.1
_**Released 06/09/2025**_
- Memory Caching Provider
- Redis Caching Provider

## OpenCQRS 7.0.0-beta.6
_**Released 05/09/2025**_
- Service Bus Provider
- RabbitMQ Provider
- Automatic publishing of messages on the back of a successfully processed command

## OpenCQRS 7.0.0-beta.5
_**Released 01/09/2025**_
- Cosmos DB store provider

## OpenCQRS 7.0.0-beta.4
_**Released 29/08/2025**_
- Send and publish methods that automatically publish notifications on the back of a successfully processed command
- Automatically validate commands before they are sent to the command handler
- Command sequences that allow to chain multiple commands in a specific order

## OpenCQRS 7.0.0-beta.3
_**Released 26/08/2025**_
- Rename track methods in the Entity Framework Core store provider
- Rename database tables in the Entity Framework Core store provider

## OpenCQRS 7.0.0-beta.2
_**Released 26/08/2025**_
- Replace events with notifications

## OpenCQRS 7.0.0-beta.1 
_**Released 25/08/2025**_
- Complete rewrite of the framework
- Upgrade to .NET 9
