using OpenCqrs.Queries;

namespace OpenCqrs.Examples.Caching.Redis.Queries;

public class GetSomethingQuery : CacheableQuery<string>;
