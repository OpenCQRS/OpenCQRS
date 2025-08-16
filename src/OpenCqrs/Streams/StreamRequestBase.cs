namespace OpenCqrs.Streams;

public abstract record StreamRequestBase<TResponse> : IStreamRequest<TResponse>;
