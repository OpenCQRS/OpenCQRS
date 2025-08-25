﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Entities;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Configurations;

public class AggregateEventEntityConfiguration : IEntityTypeConfiguration<AggregateEventEntity>
{
    public void Configure(EntityTypeBuilder<AggregateEventEntity> builder)
    {
        builder
            .ToTable(name: "AggregateEvents")
            .HasKey(aggregateEventEntity => new { aggregateEventEntity.AggregateId, aggregateEventEntity.EventId });

        builder
            .Property(aggregateEventEntity => aggregateEventEntity.AppliedDate)
            .IsRequired();
        
        builder
            .HasOne(aggregateEventEntity => aggregateEventEntity.Aggregate)
            .WithMany()
            .HasForeignKey(aggregateEventEntity => aggregateEventEntity.AggregateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(aggregateEventEntity => aggregateEventEntity.Event)
            .WithMany()
            .HasForeignKey(aggregateEventEntity => aggregateEventEntity.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(aggregateEventEntity => aggregateEventEntity.AggregateId)
            .HasDatabaseName("IX_AggregateEvents_AggregateId");
    }
}
