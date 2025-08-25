# Configuration

- [Main](#main)
- [Store Provider](#store)
- [Message Bus Provider](#bus)
- [Validation Provider](#validation)
- [Caching Provider](#caching)
- [UI](#ui)

<a name="main"></a>
## Main

First, register the main package in the ConfigureServices method of Startup.cs:

```C#
services.AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct));
```

All command, event, query and validation handlers will be registered automatically by passing one type per assembly.
CreateProduct is an sample command and GetProduct is a sample query.
In this scenario, commands and queries are in two different assemblies.
Both assemblies need to be registered.

It is possible to set a few options for the main package:

```C#
services
    .AddOpenCqrs(options =>
    {
         options.PublishEvents = true;
         options.SaveCommandData = true;
     }, typeof(CreateProduct), typeof(GetProduct));
```

**Options**

| Property | Description | Default | Notes |
| --- | --- | --- | --- |
| **PublishEvents** | The value indicating whether events are published automatically | true | The default behavior can be overridden by setting the PublishEvents property in any command |
| **SaveCommandData** | The value indicating whether domain commands data is saved | true | The default behavior can be overridden by setting the SaveCommandData property in any domain command |

<a name="store"></a>
## Store

In order to use the event sourcing functionalities you need to install and register a store provider.
The following are the providers that are currently supported:

| Package | Method |
| --- | --- |
| **OpenCqrs.Store.Cosmos.Mongo** | AddCosmosMongoStore |
| **OpenCqrs.Store.Cosmos.Sql** | AddCosmosSqlStore |
| **OpenCqrs.Store.EF.MySql** | AddMySqlStore |
| **OpenCqrs.Store.EF.PostgreSql** | AddPostgreSqlStore |
| **OpenCqrs.Store.EF.Sqlite** | AddSqliteStore |
| **OpenCqrs.Store.EF.SqlServer** | AddSqlServerStore |
| **OpenCqrs.Store.EF.Cosmos** | AddCosmosStore |

The are different registration and configuration options available for each provider:

### EF.MySql, EF.PostgreSql, EF.Sqlite and EF.SqlServer

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddSqlServerStore(options =>
    {
        options.ConnectionString = "your-connection-string";
    })
```

**Options**

| Property | Description | Default |
| --- | --- | --- |
| **ConnectionString** | The connection string of the database | _null_ |

**Confgure in Startup**

```C#
public void Configure(IApplicationBuilder app)
{
    app.UseOpenCqrs().EnsureDomainDbCreated();
}
```

### EF.Cosmos

This provider uses the new Microsoft.Azure.Cosmos package.

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosStore(options =>
    {
        options.ConnectionString = "your-connection-string";
	    options.DatabaseName = "DatabaseId";
	    options.AggregateContainerName = "Aggregates";
	    options.CommandContainerName = "Commands";
	    options.EventContainerName = "Events";
    });
```

**Options**

| Property | Description | Default |
| --- | --- | --- |
| **ConnectionString** | The service connection string | AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5...= |
| **DatabaseName** | The name of the Database | DomainStore |
| **AggregateContainerName** | The name of the Aggregate container | Aggregates |
| **CommandContainerName** | The name of the Command container | Commands |
| **EventContainerName** | The name of the Event container | Events |

Note that the partition key is set by default to '/type'.

**Configure in Startup**

```C#
public void Configure(IApplicationBuilder app)
{
    app.UseOpenCqrs().EnsureCosmosDbCreated();
}
```

### Cosmos.Sql

This provider uses the old Microsoft.Azure.DocumentDB package.

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosSqlStore(options =>
    {
	    options.ServiceEndpoint = "your-service-end-point";
	    options.AuthKey = "your-auth-key";
	    options.DatabaseId = "DatabaseId";
	    options.AggregateCollectionId = "AggregateCollectionId";
	    options.CommandCollectionId = "CommandCollectionId";
	    options.EventCollectionId = "EventCollectionId";
	    options.OfferThroughput = 400;
	    options.ConsistencyLevel = ConsistencyLevel.Session;
    });
```

**Options**

| Property | Description | Default |
| --- | --- | --- |
| **ServiceEndpoint** | The service end point | https://localhost:8081 |
| **AuthKey** | The auth key | C2y6yDjf5/R+ob0N8A7Cgv30VRDJ... |
| **DatabaseId** | The Id of the Database | DomainStore |
| **AggregateCollectionId** | The Id of the Aggregate collection | Aggregates |
| **CommandCollectionId** | The Id of the Command collection | Commands |
| **EventCollectionId** | The Id of the Event collection | Events |
| **OfferThroughput** | The offer throughput provisioned | 400 |
| **ConsistencyLevel** | The consistency level | Session |

Note that the partition key is set by default to '/type'.

**Configure in Startup**

```C#
public void Configure(IApplicationBuilder app, IOptions<DomainDbConfiguration> settings)
{
    app.UseOpenCqrs().EnsureCosmosDbSqlDbCreated(settings);
}
```

### Cosmos.Mongo

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosMongoStore(options =>
    {
        options.ConnectionString = "your-connection-string";
        options.DatabaseName = "DatabaseName";
        options.AggregateCollectionName = "AggregateCollectionName";
        options.CommandCollectionName = "CommandCollectionName";
        options.EventCollectionName = "EventCollectionName";
    });
```

**Options**

| Property | Description | Default |
| --- | --- | --- |
| **ConnectionString** | The connection string of the database | _null_ |
| **DatabaseName** | The Name of the Database | DomainStore |
| **AggregateCollectionName** | The Name of the Aggregate collection | Aggregates |
| **CommandCollectionName** | The Name of the Command collection | Commands |
| **EventCollectionName** | The Name of the Event collection | Events |

<a name="bus"></a>
## Message Bus

In order to use the message bus functionalities you need to register a message bus provider.
OpenCQRS currently supports Azure Service Bus and RabbitMQ:

| Package | Method |
| --- | --- |
| **OpenCqrs.Bus.RabbitMQ** | AddRabbitMQ |
| **OpenCqrs.Bus.ServiceBus** | AddServiceBus |

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosStore()
    .AddServiceBus(options =>
    {
        options.ConnectionString = "your-connection-string";
    });
```

**Options**

| Property | Description | Default |
| --- | --- | --- |
| **ConnectionString** | The connection string of the message bus | _null_ |

<a name="validation"></a>
## Validation

Add a validation provider if you want your commands to be validated before the command handler is executed:

| Package | Method |
| --- | --- |
| **OpenCqrs.Validation.FluentValidation** | AddFluentValidation |

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosStore()
    .AddFluentValidation(options =>
    {
        options.ValidateAllCommands = false;
    });
```

**Options**

| Property | Description | Default | Notes |
| --- | --- | --- | --- |
| **ValidateAllCommands** | The value indicating whether all commands need to be validated before being sent to the handler | false | The default value can be overridden by setting the Validate property in any command |

<a name="caching"></a>
## Caching

Add a caching provider if you want the result of your queries to be cached automatically:

| Package | Method |
| --- | --- |
| **OpenCqrs.Caching.Memory** | AddMemoryCache |
| **OpenCqrs.Caching.Redis** | AddRedisCache |

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosStore()
    .AddRedisCache(options =>
    {
        options.ConnectionString = "your-connection-string";
        options.DefaultCacheTime = 10;
    });
```

**Options**

| Property | Description | Default | Notes |
| --- | --- | --- | --- |
| **ConnectionString** | The connection string | _null_ | Redis only |
| **DefaultCacheTime** | The value indicating the default cache time (in seconds) | 60 | The default value can be overridden by setting the CacheTime property in any cacheable query |

<a name="ui"></a>
## UI

Experimental package to get a view of an aggregate and all associated events:

```C#
services
    .AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct))
    .AddCosmosStore()
    .AddUI();
```
