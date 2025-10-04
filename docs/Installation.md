# Installation

Installing the various packages:

Via Package Manager

    Install-Package OpenCqrs
   
Or via .NET CLI

    dotnet add package OpenCqrs
    
Or via Paket CLI

    paket add OpenCqrs

## Packages

| Name                                                      | Description                                        |
|-----------------------------------------------------------|----------------------------------------------------|
| OpenCqrs                                                  | Main package, all mediator features                |
| OpenCqrs.Caching.Memory                                   | Cache queries with Memory Cache                    |
| OpenCqrs.Caching.Redis                                    | Cache queries with Redis Cache                     |
| OpenCqrs.EventSourcing                                    | Main package for Event Sourcing support            |
| OpenCqrs.EventSourcing.Store.Cosmos                       | Event Sourcing with Cosmos DB                      |
| OpenCqrs.EventSourcing.Store.Cosmos.InMemory              | Event Sourcing with InMemory CosmosDB              |
| OpenCqrs.EventSourcing.Store.EntityFrameworkCore          | Event Sourcing with Entity Framework Core          |
| OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity | Event Sourcing with Entity Framework Core Identity |
| OpenCqrs.Messaging.RabbitMq                               | Messaging with RabbitMQ                            |
| OpenCqrs.Messaging.ServiceBus                             | Messaging with Service Bus                         |
| OpenCqrs.Validation.FluentValidation                      | Command validation with FluentValidation           |
