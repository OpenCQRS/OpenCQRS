using System.Threading.Tasks;
using OpenCqrs.Dependencies;

namespace OpenCqrs.Queries;

internal abstract class QueryHandlerWrapperBase<TResult>
{
    protected static THandler GetHandler<THandler>(IHandlerResolver handlerResolver)
    {
        return handlerResolver.ResolveHandler<THandler>();
    }

    public abstract Task<TResult> HandleAsync(IQuery<TResult> query, IHandlerResolver serviceFactory);
    public abstract TResult Handle(IQuery<TResult> query, IHandlerResolver serviceFactory);
}