# Installation

Installing the various packages:

Via Package Manager

    Install-Package OpenCqrs
   
Or via .NET CLI

    dotnet add package OpenCqrs
    
Or via Paket CLI

    paket add OpenCqrs

## Main package

- **OpenCqrs**

## Database provider packages

OpenCQRS currently supports the following database providers:

For CosmosDB:
- **OpenCqrs.Store.Cosmos.Sql**
- **OpenCqrs.Store.Cosmos.Mongo**

For Entity Framework Core:
- **OpenCqrs.Store.EF.SqlServer**
- **OpenCqrs.Store.EF.MySql**
- **OpenCqrs.Store.EF.PostgreSql**
- **OpenCqrs.Store.EF.Sqlite**

## Message bus provider packages

OpenCqrs currently supports the following message bus providers:
- **OpenCqrs.Bus.ServiceBus**
- **OpenCqrs.Bus.RabbitMQ**

## Validation provider package

OpenCqrs currently supports the following validation providers:
- **OpenCqrs.Validation.FluentValidation**

## Caching provider packages

OpenCqrs currently supports the following caching providers:
- **OpenCqrs.Caching.Memory**
- **OpenCqrs.Caching.Redis**

## UI package

UI experimental package:
- **OpenCqrs.UI**
