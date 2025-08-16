using System;
using OpenCqrs.Queries;
using OpenCqrs.UI.Models;

namespace OpenCqrs.UI.Queries
{
    public class GetAggregateModel : IQuery<AggregateModel>
    {
        public Guid AggregateRootId { get; set; }
    }
}
