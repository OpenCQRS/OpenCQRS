# Configuration

- [Main](#main)
- [Event Sourcing](#event-sourcing)

<a name="main"></a>
## Main

First, register OpenCQRS in the service collection (**OpenCQRS** package):

```C#
services.AddOpenCqrs(typeof(CreateProduct), typeof(GetProduct));
```

All command, event, and query handlers will be registered automatically by passing one type per assembly.
CreateProduct is an sample command and GetProduct is a sample query.
In this scenario, commands and queries are in two different assemblies.
Both assemblies need to be registered.

<a name="event-sourcing"></a>
## Event Sourcing

To use the event sourcing features, you need to install and register the event sourcing package first (**OpenCqrs.EventSourcing** package).

```C#
services.AddOpenCqrsEventSourcing();
```

Then, you need to register a store provider. The only one currently available is Entity Framework Core, but different database providers are supported. After installing the required package (**OpenCqrs.EventSourcing.Store.EntityFrameworkCore**), you can create or update your own db context and register the database provider:

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
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
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
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```
