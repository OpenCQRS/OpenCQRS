using System.Threading.Tasks;
using OpenCqrs.Dependencies;

namespace OpenCqrs.Queries
{
    internal class QueryHandlerWrapper<TQuery, TResult> : QueryHandlerWrapperBase<TResult>
        where TQuery : IQuery<TResult>
    {
        public override Task<TResult> HandleAsync(IQuery<TResult> query, IHandlerResolver handlerResolver)
        {
            var handler = GetHandler<IQueryHandlerAsync<TQuery, TResult>>(handlerResolver);
            return handler.HandleAsync((TQuery)query);
        }

        public override TResult Handle(IQuery<TResult> query, IHandlerResolver handlerResolver)
        {
            var handler = GetHandler<IQueryHandler<TQuery, TResult>>(handlerResolver);
            return handler.Handle((TQuery)query);
        }
    }
}
