using System;

namespace OpenCqrs.Store.EF.Entities
{
    public class CommandEntity
    {
        public Guid Id { get; set; }
        public Guid AggregateId { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public DateTime TimeStamp { get; set; }
        public string UserId { get; set; }
        public string Source { get; set; }
    }
}
