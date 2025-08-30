using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenCqrs.EventSourcing.Data;

namespace OpenCqrs.EventSourcing.Store.EntityFrameworkCore.Configurations;

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
            .Property(eventEntity => eventEntity.TypeName)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(eventEntity => eventEntity.CreatedDate)
            .IsRequired();

        builder
            .Property(eventEntity => eventEntity.CreatedBy)
            .HasMaxLength(255);

        builder
            .HasIndex(eventEntity => eventEntity.StreamId)
            .HasDatabaseName("IX_Events_StreamId");

        builder
            .HasIndex(eventEntity => new { eventEntity.StreamId, eventEntity.Sequence })
            .HasDatabaseName("IX_Events_StreamId_Sequence");

        builder
            .HasIndex(eventEntity => new { eventEntity.StreamId, eventEntity.TypeName, eventEntity.TypeVersion })
            .HasDatabaseName("IX_Events_StreamId_Type");
    }
}
