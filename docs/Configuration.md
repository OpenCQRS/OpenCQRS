# Configuration

- [Main](#main)
- [Command Validation](#command-validation)
- [Messaging](#messaging)
- [Caching](#caching)
- [Event Sourcing](#event-sourcing)

<a name="main"></a>
## Main
First, register OpenCQRS in the service collection (**OpenCQRS** package):
```C#
services.AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct));
```
All command, event, and query handlers will be registered automatically by passing one type per assembly.
CreateProduct is a sample command, and GetProduct is a sample query.
In this scenario, commands and queries are in two different assemblies.
Both assemblies need to be registered.

<a name="command-validation"></a>
## Command Validation
To use the command validation features, you need to install and register a validation package first.

**OpenCqrs.Validation.FluentValidation**
```C#
services.AddOpenCqrsFluentValidation(typeof(CreateProduct));
```
All validators will be registered automatically by passing one type per assembly.

<a name="messaging"></a>
## Messaging
To use the messaging features, you need to install and register a messaging package first.

**OpenCqrs.Messaging.ServiceBus**
```C#
services.AddOpenCqrsServiceBus(new ServiceBusOptions { ConnectionString = connectionString });
```
**OpenCqrs.Messaging.RabbitMq**
```C#
services
    .AddOpenCqrsRabbitMq(options =>
    {
         options.ConnectionString = connectionString;
     });
```

<a name="caching"></a>
## Caching
To use the caching features, you need to install and register a caching package.

**OpenCqrs.Caching.Memory**
```C#
services.AddOpenCqrsMemoryCache();
```
**OpenCqrs.Caching.Redis**
```C#
services.AddOpenCqrsRedisCache(options =>
{
    options.ConnectionString = "localhost:6379"
});
```

<a name="event-sourcing"></a>
## Event Sourcing
To use the event sourcing features, you need to install and register the event sourcing package first (**OpenCqrs.EventSourcing** package).
```C#
services.AddOpenCqrsEventSourcing();
```
Then, you need to register a store provider.
### Entity Framework Core Store Provider
After installing the required package (**OpenCqrs.EventSourcing.Store.EntityFrameworkCore**), you can create or update your own db context and register the database provider:
```C#
// Your db context that inherits from DomainDbContext
public class ApplicationDbContext(
    DbContextOptions<DomainDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : DomainDbContext(options, timeProvider, httpContextAccessor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
    
    public DbSet<ItemEntity> Items { get; set; } = null!;
}

// Register the db context with the provider of your choice
services
    .AddScoped(sp => new DbContextOptionsBuilder<DomainDbContext>()
        .UseSqlite(connectionString)
        .UseApplicationServiceProvider(sp)
        .Options);
    
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

// Register the event sourcing store provider
services.AddOpenCqrsEntityFrameworkCore<ApplicationDbContext>();
```
OpenCQRS also supports ASP.NET Core Identity. Install the **OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity** package and use the IdentityDomainDbContext in your application:
```C#
// Your db context that inherits from DomainDbContext
public class ApplicationDbContext(
    DbContextOptions<DomainDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDomainDbContext(options, timeProvider, httpContextAccessor)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
    
    public DbSet<ItemEntity> Items { get; set; } = null!;
}

// Register identity
services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Register the db context with the provider of your choice
services
    .AddScoped(sp => new DbContextOptionsBuilder<DomainDbContext>()
        .UseSqlite(connectionString)
        .UseApplicationServiceProvider(sp)
        .Options);

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

// Register the event sourcing store provider
services.AddOpenCqrsEntityFrameworkCore<ApplicationDbContext>();
```

### Cosmo DB Store Provider
After installing the required package (**OpenCqrs.EventSourcing.Store.Cosmos**), you can register the Cosmo DB store provider:
```C#
services.AddOpenCqrsCosmos(options =>
{
    // Required
    options.Endpoint = "your-cosmosdb-endpoint";
    
    // Required
    options.AuthKey = "your-cosmosdb-auth-key";
    
    // Optional, default is "OpenCQRS"
    options.DatabaseName = "your-database-name"; 
    
    // Optional, default is "Domain"
    options.ContainerName = "your-container-name"; 
    
    // Optional, default is new CosmosClientOptions()
    // with ApplicationName set to "OpenCqrs"
    // and ConnectionMode set to ConnectionMode.Direct
    options.ClientOptions = new CosmosClientOptions(); 
});
 ```

You can use the `CosmosSetup` helper to create the database and the container if they do not exist:
```C#
cosmosSetup.CreateDatabaseAndContainerIfNotExist(throughput = 400);
```
