using OpenCqrs.Queries;

namespace OpenCqrs.Tests.Fakes
{
    public class GetSomethingCacheable : CacheableQuery<Something>
    {
        public GetSomethingCacheable()
        {
            CacheKey = "SomethingCached";
        }
    }
}
