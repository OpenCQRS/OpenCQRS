# OpenCQRS

OpenCQRS is a .NET Core framework that can be used to create a simple and clean design by enforcing single responsibility and separation of concerns.

Its advanced features are ideal for Domain Driven Design (DDD), Command Query Responsibilty Segragation (CQRS) and Event Sourcing.

With OpenCQRS you can automatically dispatch events to a message bus (Service Bus or RabbitMQ), validate your commands before they are sent to the command handler and automatically cache the result of your queries (Memory or Redis).

**Repository**: [https://github.com/lucabriguglia/OpenCQRS](https://github.com/lucabriguglia/OpenCQRS)

## Documentation

- [Installation](Installation)
- [Configuration](Configuration)
- [Basics](Basics)
   - [Commands](Commands)
   - [Events](Events)
   - [Queries](Queries)
- [Domain Commands](Domain-Commands)
   - [With Event Sourcing](With-Event-Sourcing)
   - [Without Event Sourcing](Without-Event-Sourcing)
- [Command Validation](Command-Validation)
- [Command Sequence](Command-Sequence)
- [Message Bus](Message-Bus)
- [Caching Queries](Caching-Queries)
- [UI](UI)
- [Providers](Providers)
- [Samples](Samples)
- [Release Notes](Release-Notes)
