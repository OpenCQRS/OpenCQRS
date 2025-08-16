using System.Threading.Tasks;
using OpenCqrs.Domain;
using OpenCqrs.Queries;
using OpenCqrs.UI.Models;

namespace OpenCqrs.UI.Queries.Handlers
{
    public class GetAggregateModelHandler : IQueryHandlerAsync<GetAggregateModel, AggregateModel>
    {
        private readonly IStoreProvider _storeProvider;

        public GetAggregateModelHandler(IStoreProvider storeProvider)
        {
            _storeProvider = storeProvider;
        }

        public async Task<AggregateModel> HandleAsync(GetAggregateModel query)
        {
            var events = await _storeProvider.GetEventsAsync(query.AggregateRootId);
            var aggregate = new AggregateModel(events);
            return aggregate;
        }
    }
}
