using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReflectionMagic;

namespace OpenCqrs.Domain
{
    public abstract class AggregateRoot : IAggregateRoot
    {
        public Guid Id { get; protected set; }
        public int Version { get; private set; }

        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();
        public ReadOnlyCollection<IDomainEvent> Events => _events.AsReadOnly();

        protected AggregateRoot()
        {
            Id = Guid.NewGuid();
        }

        protected AggregateRoot(Guid id)
        {
            if (id == Guid.Empty)
                id = Guid.NewGuid();

            Id = id;
        }

        public void LoadsFromHistory(IEnumerable<IDomainEvent> events)
        {
            var domainEvents = events as IDomainEvent[] ?? events.ToArray();

            foreach (var @event in domainEvents)
            {
                ApplyEvent(@event);
            }
        }

        /// <summary>
        /// Adds the event to the new events collection.
        /// </summary>
        /// <param name="event">The event.</param>
        protected void AddEvent(IDomainEvent @event)
        {
            _events.Add(@event);
        }

        private void ApplyEvent(IDomainEvent @event)
        {
            this.AsDynamic().Apply(@event);
            Version++;
        }

        /// <summary>
        /// Adds the event to the new events collection and calls the related apply method.
        /// </summary>
        /// <param name="event">The event.</param>
        protected void AddAndApplyEvent(IDomainEvent @event)
        {
            AddEvent(@event);
            ApplyEvent(@event);
        }
    }
}
