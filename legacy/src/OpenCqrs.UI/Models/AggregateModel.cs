using System.Collections.Generic;
using System.Linq;
using OpenCqrs.Domain;

namespace OpenCqrs.UI.Models
{
    public class AggregateModel
    {
        public AggregateModel(IEnumerable<IDomainEvent> events)
        {
            var domainEvents = events as IDomainEvent[] ?? events.OrderByDescending(x => x.TimeStamp).ToArray();

            foreach (var @event in domainEvents)
            {
                Events.Add(new EventModel(@event));
                Version++;
            }
        }

        public IList<EventModel> Events { get; set; } = new List<EventModel>();
        public int Version { get; set; }
    }
}
