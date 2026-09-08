using Memoria.EventSourcing.Store.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Memoria.EventSourcing.Store.EntityFrameworkCore.Configurations;

public class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder
            .ToTable(name: "DomainEvents")
            .HasKey(eventEntity => eventEntity.Id);

        builder
            .Property(eventEntity => eventEntity.StreamId)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(eventEntity => eventEntity.EventType)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(eventEntity => eventEntity.CreatedDate)
            .IsRequired();

        builder
            .Property(eventEntity => eventEntity.CreatedBy)
            .HasMaxLength(255);

        // A stream's events are always read in sequence order, so this index serves the StreamId-only
        // lookups too — a separate index on StreamId alone would be a prefix of this one and cost
        // maintenance on every append for nothing.
        //
        // Unique because it is the natural key: EventEntity.Id is derived as "{StreamId}:{Sequence}",
        // so the primary key already made this pair unique. Declaring it lets the database enforce
        // the invariant the store's read-then-write sequence check assumes.
        builder
            .HasIndex(eventEntity => new { eventEntity.StreamId, eventEntity.Sequence })
            .IsUnique()
            .HasDatabaseName("IX_Events_StreamId_Sequence");

        // Serves the from/up-to/between-date reads, which previously had to scan a whole stream.
        builder
            .HasIndex(eventEntity => new { eventEntity.StreamId, eventEntity.CreatedDate })
            .HasDatabaseName("IX_Events_StreamId_CreatedDate");

        builder
            .HasIndex(eventEntity => eventEntity.EventType)
            .HasDatabaseName("IX_Events_EventType");
    }
}
