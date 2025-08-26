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

- [Extensions](Entity-Framework-Core-Extensions)
- [Scenarios](Entity-Framework-Core-Scenarios)
