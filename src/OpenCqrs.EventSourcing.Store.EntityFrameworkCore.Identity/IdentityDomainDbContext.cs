﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Domain;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Configurations;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Interceptors;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Identity;

public abstract class IdentityDomainDbContext(
    DbContextOptions<IdentityDomainDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<IdentityUser>(options), IDomainDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.AddInterceptors(new AuditInterceptor(timeProvider, httpContextAccessor));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Add option to customise table names (Issue #121)

        modelBuilder.ApplyConfiguration(new AggregateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AggregateEventEntityConfiguration());
    }

    public DbSet<AggregateEntity> Aggregates { get; set; } = null!;
    public DbSet<EventEntity> Events { get; set; } = null!;
    public DbSet<AggregateEventEntity> AggregateEvents { get; set; } = null!;

    public void DetachAggregate<TAggregate>(IAggregateId aggregateId, TAggregate aggregate) where TAggregate : IAggregate
    {
        foreach (var entityEntry in ChangeTracker.Entries())
        {
            if (entityEntry.Entity is not AggregateEntity aggregateEntity)
            {
                continue;
            }

            if (aggregateEntity.Id == aggregateId.ToIdWithTypeVersion(aggregate.AggregateType().Version))
            {
                entityEntry.State = EntityState.Detached;
            }
        }
    }
}
