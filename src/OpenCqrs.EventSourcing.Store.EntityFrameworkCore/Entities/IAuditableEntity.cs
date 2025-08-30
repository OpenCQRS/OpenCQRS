namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

/// <summary>
/// Defines the contract for entities that require creation audit trail functionality in the Entity Framework Core event store.
/// Provides automatic tracking of when and by whom entities were initially created, supporting compliance,
/// debugging, and operational monitoring requirements.
/// </summary>
/// <example>
/// <code>
/// // Example entity implementing IAuditableEntity
/// public class CustomerEntity : IAuditableEntity
/// {
///     public string Id { get; set; } = null!;
///     public string Name { get; set; } = null!;
///     public string Email { get; set; } = null!;
///     
///     // Audit properties - automatically populated by infrastructure
///     public DateTimeOffset CreatedDate { get; set; }
///     public string? CreatedBy { get; set; }
/// }
/// 
/// // Entity Framework configuration
/// public void Configure(EntityTypeBuilder&lt;CustomerEntity&gt; builder)
/// {
///     builder.HasKey(e =&gt; e.Id);
///     
///     // Configure audit properties
///     builder.Property(e =&gt; e.CreatedDate)
///         .IsRequired()
///         .HasComment("Timestamp when the entity was created");
///         
///     builder.Property(e =&gt; e.CreatedBy)
///         .HasMaxLength(256)
///         .HasComment("Identifier of the user or system that created the entity");
/// }
/// 
/// // AuditInterceptor automatically populates these fields
/// public class CustomerRepository
/// {
///     private readonly DbContext _context;
///     
///     public CustomerRepository(DbContext context)
///     {
///         _context = context;
///     }
///     
///     public async Task&lt;CustomerEntity&gt; CreateCustomerAsync(string name, string email)
///     {
///         var customer = new CustomerEntity
///         {
///             Id = Guid.NewGuid().ToString(),
///             Name = name,
///             Email = email
///             // CreatedDate and CreatedBy are set automatically by AuditInterceptor
///         };
///         
///         _context.Customers.Add(customer);
///         await _context.SaveChangesAsync(); // Audit fields populated here
///         
///         return customer;
///     }
/// }
/// 
/// // Querying entities with audit information
/// public async Task&lt;List&lt;CustomerEntity&gt;&gt; GetRecentCustomersAsync(TimeSpan timeSpan)
/// {
///     var cutoffDate = DateTimeOffset.UtcNow.Subtract(timeSpan);
///     
///     return await _context.Customers
///         .Where(c =&gt; c.CreatedDate &gt;= cutoffDate)
///         .OrderByDescending(c =&gt; c.CreatedDate)
///         .ToListAsync();
/// }
/// 
/// // Audit reporting example
/// public async Task&lt;Dictionary&lt;string, int&gt;&gt; GetCreationStatsByUserAsync(DateTime startDate, DateTime endDate)
/// {
///     var startOffset = new DateTimeOffset(startDate, TimeSpan.Zero);
///     var endOffset = new DateTimeOffset(endDate, TimeSpan.Zero);
///     
///     return await _context.Customers
///         .Where(c =&gt; c.CreatedDate &gt;= startOffset && c.CreatedDate &lt; endOffset)
///         .GroupBy(c =&gt; c.CreatedBy ?? "System")
///         .Select(g =&gt; new { User = g.Key, Count = g.Count() })
///         .ToDictionaryAsync(x =&gt; x.User, x =&gt; x.Count);
/// }
/// 
/// // Service registration for dependency injection
/// public void ConfigureServices(IServiceCollection services)
/// {
///     services.AddSingleton(TimeProvider.System);
///     services.AddScoped&lt;AuditInterceptor&gt;();
///     
///     services.AddDbContext&lt;EventStoreContext&gt;(options =&gt;
///     {
///         options.UseSqlServer(connectionString);
///         options.AddInterceptors&lt;AuditInterceptor&gt;();
///     });
/// }
/// </code>
/// </example>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the timestamp when this entity was initially created and persisted to the database.
    /// This field is automatically populated by the audit infrastructure and should never be modified after creation.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the entity was first saved to the database.
    /// The value uses UTC time for consistency across different time zones and deployment environments.
    /// </value>
    DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system that initially created this entity.
    /// This field is automatically populated by the audit infrastructure based on the current security context.
    /// </summary>
    /// <value>
    /// A string that identifies the user, service account, or system that created the entity.
    /// Can be null if the creator information is not available or if the entity was created by system processes.
    /// </value>
    string? CreatedBy { get; set; }
}
