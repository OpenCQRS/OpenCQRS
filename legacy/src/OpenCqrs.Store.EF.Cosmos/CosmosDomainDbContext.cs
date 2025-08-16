using Microsoft.EntityFrameworkCore;
using OpenCqrs.Store.EF.Cosmos.Configuration;
using OpenCqrs.Store.EF.Entities;

namespace OpenCqrs.Store.EF.Cosmos
{
    public class CosmosDomainDbContext : DomainDbContext
    {
        private readonly CosmosDbOptions _settings;

        public CosmosDomainDbContext(DbContextOptions<DomainDbContext> options, CosmosDbOptions settings)
            : base(options)
        {
            _settings = settings;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            SetAggregate(builder);
            SetCommand(builder);
            SetEvent(builder);
        }

        private void SetAggregate(ModelBuilder builder)
        {
            builder.Entity<AggregateEntity>()
                .ToContainer(_settings.AggregateContainerName)
                .HasPartitionKey(p => p.Type)
                .HasNoDiscriminator();

            builder.Entity<AggregateEntity>()
                .Property(p => p.Id)
                .ToJsonProperty("id");

            builder.Entity<AggregateEntity>()
                .Property(p => p.Type)
                .ToJsonProperty("type");
        }

        private void SetCommand(ModelBuilder builder)
        {
            builder.Entity<CommandEntity>()
                .ToContainer(_settings.CommandContainerName)
                .HasPartitionKey(p => p.Type)
                .HasNoDiscriminator();

            builder.Entity<CommandEntity>()
                .Property(p => p.Id)
                .ToJsonProperty("id");

            builder.Entity<CommandEntity>()
                .Property(p => p.AggregateId)
                .ToJsonProperty("aggregateId");

            builder.Entity<CommandEntity>()
                .Property(p => p.Type)
                .ToJsonProperty("type");

            builder.Entity<CommandEntity>()
                .Property(p => p.Data)
                .ToJsonProperty("data");

            builder.Entity<CommandEntity>()
                .Property(p => p.TimeStamp)
                .ToJsonProperty("timeStamp");

            builder.Entity<CommandEntity>()
                .Property(p => p.UserId)
                .ToJsonProperty("userId");

            builder.Entity<CommandEntity>()
                .Property(p => p.Source)
                .ToJsonProperty("source");
        }

        private void SetEvent(ModelBuilder builder)
        {
            builder.Entity<EventEntity>()
                .ToContainer(_settings.EventContainerName)
                .HasPartitionKey(p => p.Type)
                .HasNoDiscriminator();

            builder.Entity<EventEntity>()
                .Property(p => p.Id)
                .ToJsonProperty("id");

            builder.Entity<EventEntity>()
                .Property(p => p.AggregateId)
                .ToJsonProperty("aggregateId");

            builder.Entity<EventEntity>()
                .Property(p => p.CommandId)
                .ToJsonProperty("commandId");

            builder.Entity<EventEntity>()
                .Property(p => p.Sequence)
                .ToJsonProperty("sequence");

            builder.Entity<EventEntity>()
                .Property(p => p.Type)
                .ToJsonProperty("type");

            builder.Entity<EventEntity>()
                .Property(p => p.Data)
                .ToJsonProperty("data");

            builder.Entity<EventEntity>()
                .Property(p => p.TimeStamp)
                .ToJsonProperty("timeStamp");

            builder.Entity<EventEntity>()
                .Property(p => p.UserId)
                .ToJsonProperty("userId");

            builder.Entity<EventEntity>()
                .Property(p => p.Source)
                .ToJsonProperty("source");
        }
    }
}
