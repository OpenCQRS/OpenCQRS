using System.Collections.Generic;

namespace OpenCqrs.Bus
{
    public interface IBusMessage
    {
        IDictionary<string, object> Properties { get; set; }
    }
}
