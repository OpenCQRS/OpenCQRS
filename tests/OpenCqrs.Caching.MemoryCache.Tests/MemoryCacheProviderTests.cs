using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace OpenCqrs.Caching.MemoryCache.Tests;

public class MemoryCacheProviderTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheProvider _provider;

    public MemoryCacheProviderTests()
    {
        _memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        var defaultOptions = new Configuration.MemoryCacheOptions { DefaultCacheTimeInSeconds = 300 };
        var options = Substitute.For<IOptions<Configuration.MemoryCacheOptions>>();
        options.Value.Returns(defaultOptions);
        _provider = new MemoryCacheProvider(_memoryCache, options);
    }

    // TODO: Add tests
   
    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    private class TestComplexObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
