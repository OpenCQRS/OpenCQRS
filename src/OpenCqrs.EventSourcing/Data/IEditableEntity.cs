namespace OpenCqrs.EventSourcing.Data;

/// <summary>
/// Defines the contract for entities that require modification audit trail functionality in the Entity Framework Core event store.
/// Provides automatic tracking of when and by whom entities were last updated, supporting compliance,
/// debugging, and operational monitoring requirements for entities that can be modified after creation.
/// </summary>
/// <example>
/// <code>
/// // Example entity implementing both audit interfaces
/// public class CustomerProjectionEntity : IAuditableEntity, IEditableEntity
/// {
///     public string Id { get; set; } = null!;
///     public string Name { get; set; } = null!;
///     public string Email { get; set; } = null!;
///     public int Version { get; set; }
///     
///     // Creation audit properties - set once
///     public DateTimeOffset CreatedDate { get; set; }
///     public string? CreatedBy { get; set; }
///     
///     // Modification audit properties - updated on each save
///     public DateTimeOffset UpdatedDate { get; set; }
///     public string? UpdatedBy { get; set; }
/// }
/// 
/// // Entity Framework configuration
/// public void Configure(EntityTypeBuilder&lt;CustomerProjectionEntity&gt; builder)
/// {
///     builder.HasKey(e =&gt; e.Id);
///     
///     // Configure creation audit properties
///     builder.Property(e =&gt; e.CreatedDate)
///         .IsRequired()
///         .HasComment("Timestamp when the entity was created");
///         
///     builder.Property(e =&gt; e.CreatedBy)
///         .HasMaxLength(256)
///         .HasComment("Identifier of the user or system that created the entity");
///     
///     // Configure modification audit properties
///     builder.Property(e =&gt; e.UpdatedDate)
///         .IsRequired()
///         .HasComment("Timestamp when the entity was last updated");
///         
///     builder.Property(e =&gt; e.UpdatedBy)
///         .HasMaxLength(256)
///         .HasComment("Identifier of the user or system that last updated the entity");
///         
///     // Index for temporal queries
///     builder.HasIndex(e =&gt; e.UpdatedDate)
///         .HasDatabaseName("IX_CustomerProjection_UpdatedDate");
/// }
/// 
/// // Repository with automatic audit field handling
/// public class CustomerProjectionRepository
/// {
///     private readonly DbContext _context;
///     
///     public CustomerProjectionRepository(DbContext context)
///     {
///         _context = context;
///     }
///     
///     public async Task&lt;CustomerProjectionEntity&gt; CreateAsync(string name, string email)
///     {
///         var customer = new CustomerProjectionEntity
///         {
///             Id = Guid.NewGuid().ToString(),
///             Name = name,
///             Email = email,
///             Version = 1
///             // Audit fields are set automatically by interceptors
///         };
///         
///         _context.CustomerProjections.Add(customer);
///         await _context.SaveChangesAsync(); // Both creation and update audit fields populated
///         
///         return customer;
///     }
///     
///     public async Task&lt;CustomerProjectionEntity&gt; UpdateAsync(string id, string newName, string newEmail)
///     {
///         var customer = await _context.CustomerProjections.FindAsync(id);
///         if (customer == null)
///             throw new EntityNotFoundException($"Customer {id} not found");
///         
///         customer.Name = newName;
///         customer.Email = newEmail;
///         customer.Version++;
///         
///         // UpdatedDate and UpdatedBy are set automatically by audit interceptor
///         await _context.SaveChangesAsync();
///         
///         return customer;
///     }
/// }
/// 
/// // Audit interceptor for automatic field population
/// public class ModificationAuditInterceptor : SaveChangesInterceptor
/// {
///     private readonly TimeProvider _timeProvider;
///     private readonly IHttpContextAccessor _httpContextAccessor;
///     
///     public ModificationAuditInterceptor(TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor)
///     {
///         _timeProvider = timeProvider;
///         _httpContextAccessor = httpContextAccessor;
///     }
///     
///     public override InterceptionResult&lt;int&gt; SavingChanges(DbContextEventData eventData, InterceptionResult&lt;int&gt; result)
///     {
///         UpdateAuditFields(eventData.Context);
///         return base.SavingChanges(eventData, result);
///     }
///     
///     public override ValueTask&lt;InterceptionResult&lt;int&gt;&gt; SavingChangesAsync(
///         DbContextEventData eventData, 
///         InterceptionResult&lt;int&gt; result, 
///         CancellationToken cancellationToken = default)
///     {
///         UpdateAuditFields(eventData.Context);
///         return base.SavingChangesAsync(eventData, result, cancellationToken);
///     }
///     
///     private void UpdateAuditFields(DbContext? context)
///     {
///         if (context == null) return;
///         
///         var currentTime = _timeProvider.GetUtcNow();
///         var currentUser = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
///         
///         foreach (var entry in context.ChangeTracker.Entries())
///         {
///             // Handle creation audit for new entities
///             if (entry.State == EntityState.Added && entry.Entity is IAuditableEntity auditableEntity)
///             {
///                 auditableEntity.CreatedDate = currentTime;
///                 auditableEntity.CreatedBy = currentUser;
///             }
///             
///             // Handle modification audit for updated entities
///             if ((entry.State == EntityState.Added || entry.State == EntityState.Modified) && 
///                 entry.Entity is IEditableEntity editableEntity)
///             {
///                 editableEntity.UpdatedDate = currentTime;
///                 editableEntity.UpdatedBy = currentUser;
///             }
///         }
///     }
/// }
/// 
/// // Querying entities with modification audit information
/// public async Task&lt;List&lt;CustomerProjectionEntity&gt;&gt; GetRecentlyModifiedAsync(TimeSpan timeSpan)
/// {
///     var cutoffDate = DateTimeOffset.UtcNow.Subtract(timeSpan);
///     
///     return await _context.CustomerProjections
///         .Where(c =&gt; c.UpdatedDate &gt;= cutoffDate)
///         .OrderByDescending(c =&gt; c.UpdatedDate)
///         .ToListAsync();
/// }
/// 
/// // Modification audit reporting
/// public async Task&lt;Dictionary&lt;string, int&gt;&gt; GetModificationStatsByUserAsync(DateTime startDate, DateTime endDate)
/// {
///     var startOffset = new DateTimeOffset(startDate, TimeSpan.Zero);
///     var endOffset = new DateTimeOffset(endDate, TimeSpan.Zero);
///     
///     return await _context.CustomerProjections
///         .Where(c =&gt; c.UpdatedDate &gt;= startOffset && c.UpdatedDate &lt; endOffset)
///         .GroupBy(c =&gt; c.UpdatedBy ?? "System")
///         .Select(g =&gt; new { User = g.Key, Count = g.Count() })
///         .ToDictionaryAsync(x =&gt; x.User, x =&gt; x.Count);
/// }
/// 
/// // Finding entities modified by specific user
/// public async Task&lt;List&lt;CustomerProjectionEntity&gt;&gt; GetEntitiesModifiedByUserAsync(
///     string userId, 
///     DateTime? since = null)
/// {
///     var query = _context.CustomerProjections.Where(c =&gt; c.UpdatedBy == userId);
///     
///     if (since.HasValue)
///     {
///         var sinceOffset = new DateTimeOffset(since.Value, TimeSpan.Zero);
///         query = query.Where(c =&gt; c.UpdatedDate &gt;= sinceOffset);
///     }
///     
///     return await query
///         .OrderByDescending(c =&gt; c.UpdatedDate)
///         .ToListAsync();
/// }
/// 
/// // Service registration for dependency injection
/// public void ConfigureServices(IServiceCollection services)
/// {
///     services.AddSingleton(TimeProvider.System);
///     services.AddHttpContextAccessor();
///     services.AddScoped&lt;ModificationAuditInterceptor&gt;();
///     
///     services.AddDbContext&lt;EventStoreContext&gt;(options =&gt;
///     {
///         options.UseSqlServer(connectionString);
///         options.AddInterceptors&lt;ModificationAuditInterceptor&gt;();
///     });
/// }
/// 
/// // Example of aggregate entity that gets updated (snapshot pattern)
/// public class OrderAggregateEntity : IAuditableEntity, IEditableEntity, IBindableEntity
/// {
///     public string Id { get; set; } = null!;
///     public string StreamId { get; set; } = null!;
///     public int Version { get; set; }
///     public string Data { get; set; } = null!;
///     
///     // Creation audit - set once when aggregate is first snapshotted
///     public DateTimeOffset CreatedDate { get; set; }
///     public string? CreatedBy { get; set; }
///     
///     // Modification audit - updated each time snapshot is refreshed
///     public DateTimeOffset UpdatedDate { get; set; }
///     public string? UpdatedBy { get; set; }
///     
///     // Type binding for serialization
///     public string TypeName { get; set; } = null!;
///     public int TypeVersion { get; set; }
/// }
/// </code>
/// </example>
public interface IEditableEntity
{
    /// <summary>
    /// Gets or sets the timestamp when this entity was last modified and persisted to the database.
    /// This field is automatically updated by the audit infrastructure on every save operation
    /// that modifies the entity, providing a complete trail of when changes occurred.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the entity was most recently updated.
    /// The value uses UTC time for consistency across different time zones and deployment environments.
    /// </value>
    DateTimeOffset UpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system that last modified this entity.
    /// This field is automatically updated by the audit infrastructure based on the current
    /// security context during every save operation that modifies the entity.
    /// </summary>
    /// <value>
    /// A string that identifies the user, service account, or system that most recently modified the entity.
    /// Can be null if the modifier information is not available or if the entity was modified by system processes.
    /// </value>
    string? UpdatedBy { get; set; }
}
