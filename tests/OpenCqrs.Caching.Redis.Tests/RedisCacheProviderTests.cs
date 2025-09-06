
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Newtonsoft.Json;
using OpenCqrs.Caching.Redis;
using OpenCqrs.Caching.Redis.Configuration;
using StackExchange.Redis;
using Xunit;

namespace OpenCqrs.Caching.Redis.Tests;

public class RedisCacheProviderTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly IOptions<RedisCacheOptions> _options;
    private readonly RedisCacheProvider _provider;
    private readonly RedisCacheOptions _defaultOptions;

    public RedisCacheProviderTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _defaultOptions = new RedisCacheOptions 
        { 
            DefaultCacheTimeInSeconds = 300,
            ConnectionString = "localhost:6379"
        };
        _options = Substitute.For<IOptions<RedisCacheOptions>>();
        _options.Value.Returns(_defaultOptions);
        
        _connectionMultiplexer
            .GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns(_database);

        _provider = new RedisCacheProvider(_connectionMultiplexer, _options);
    }
    
    // TODO: Add tests

    private class TestComplexObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}
