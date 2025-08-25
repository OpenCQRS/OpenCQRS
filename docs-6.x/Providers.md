# Providers

It is possible to customise OpenCQRS by creating a custom provider.
Thre are four different providers and for each of them there is only one interface to implement in order to create a custom one.

| Provider | Description | Interface | Example
| --- | --- | --- | --- |
| **Store** | Stores domain data | _IStoreProvider_ | [CosmosDB Mongo API](https://github.com/lucabriguglia/OpenCQRS/tree/master/src/OpenCqrs.Store.Cosmos.Mongo) |
| **Bus** | Sends events to a message bus | _IBusProvider_ | [RabbitMQ](https://github.com/lucabriguglia/OpenCQRS/tree/master/src/OpenCqrs.Bus.RabbitMQ) |
| **Caching** | Gets query results from cache | _ICacheProvider_ | [Redis](https://github.com/lucabriguglia/OpenCQRS/tree/master/src/OpenCqrs.Caching.Redis) |
| **Validation** | Validates commands | _IValidationProvider_ | [FluentValidation](https://github.com/lucabriguglia/OpenCQRS/tree/master/src/OpenCqrs.Validation.FluentValidation) |
