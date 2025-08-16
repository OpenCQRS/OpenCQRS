using System;

namespace OpenCqrs.Events
{
    public interface IEvent
    {
        string UserId { get; set; }
        string Source { get; set; }
        DateTime TimeStamp { get; set; }
    }
}
