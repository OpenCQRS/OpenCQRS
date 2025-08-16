using System;

namespace OpenCqrs.Dependencies
{
    public interface IHandlerResolver
    {
        THandler ResolveHandler<THandler>();
        object ResolveHandler(Type handlerType);
        object ResolveHandler(object param, Type type);
        object ResolveQueryHandler(object query, Type type);
    }
}
