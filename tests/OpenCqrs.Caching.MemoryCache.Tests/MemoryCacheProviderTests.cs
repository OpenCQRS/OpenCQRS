using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace OpenCqrs.Caching.MemoryCache.Tests;

public class MemoryCacheProviderTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IOptions<Configuration.MemoryCacheOptions>> _optionsMock;
    private readonly MemoryCacheProvider _provider;

    public MemoryCacheProviderTests()
    {
        _memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        var defaultOptions = new Configuration.MemoryCacheOptions { DefaultCacheTimeInSeconds = 300 };
        _optionsMock = new Mock<IOptions<Configuration.MemoryCacheOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(defaultOptions);
        _provider = new MemoryCacheProvider(_memoryCache, _optionsMock.Object);
    }

    [Fact]
    public async Task Get_WithValidKey_ShouldReturnCachedValue()
    {
        const string key = "test-key";
        const string expectedValue = "test-value";
        _memoryCache.Set(key, expectedValue);

        var result = await _provider.Get<string>(key);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task Get_WithNonExistentKey_ShouldReturnNull()
    {
        const string key = "non-existent-key";

        var result = await _provider.Get<string>(key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Set_WithValidData_ShouldCacheValue()
    {
        const string key = "test-key";
        const string value = "test-value";

        await _provider.Set(key, value);

        var cachedValue = _memoryCache.Get<string>(key);
        cachedValue.Should().Be(value);
    }

    [Fact]
    public async Task Set_WithNullData_ShouldNotCacheAnything()
    {
        const string key = "test-key";

        await _provider.Set(key, null);

        var isSet = await _provider.IsSet(key);
        isSet.Should().BeFalse();
    }

    [Fact]
    public async Task Set_WithExistingKey_ShouldNotOverwrite()
    {
        const string key = "test-key";
        const string originalValue = "original-value";
        const string newValue = "new-value";

        await _provider.Set(key, originalValue);

        await _provider.Set(key, newValue);

        var cachedValue = await _provider.Get<string>(key);
        cachedValue.Should().Be(originalValue);
    }

    [Fact]
    public async Task Set_WithCustomCacheTime_ShouldUseSpecifiedTime()
    {
        const string key = "test-key";
        const string value = "test-value";
        const int customCacheTime = 60;

        await _provider.Set(key, value, customCacheTime);

        var isSet = await _provider.IsSet(key);
        isSet.Should().BeTrue();
        
        var cachedValue = await _provider.Get<string>(key);
        cachedValue.Should().Be(value);
    }

    [Fact]
    public async Task Set_WithoutCacheTime_ShouldUseDefaultCacheTime()
    {
        const string key = "test-key";
        const string value = "test-value";

        await _provider.Set(key, value);

        var isSet = await _provider.IsSet(key);
        isSet.Should().BeTrue();

        _optionsMock.Verify(x => x.Value, Times.AtLeastOnce);
    }

    [Fact]
    public async Task IsSet_WithExistingKey_ShouldReturnTrue()
    {
        const string key = "test-key";
        const string value = "test-value";
        _memoryCache.Set(key, value);

        var result = await _provider.IsSet(key);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSet_WithNonExistentKey_ShouldReturnFalse()
    {
        const string key = "non-existent-key";

        var result = await _provider.IsSet(key);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_WithExistingKey_ShouldRemoveFromCache()
    {
        const string key = "test-key";
        const string value = "test-value";
        _memoryCache.Set(key, value);

        var isSetBefore = await _provider.IsSet(key);
        isSetBefore.Should().BeTrue();

        await _provider.Remove(key);

        var isSetAfter = await _provider.IsSet(key);
        isSetAfter.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_WithNonExistentKey_ShouldNotThrow()
    {
        const string key = "non-existent-key";

        var act = async () => await _provider.Remove(key);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("test-key")]
    [InlineData("test:key:with:colons")]
    [InlineData("test.key.with.dots")]
    public async Task Set_WithVariousKeyFormats_ShouldWork(string key)
    {
        const string value = "test-value";

        await _provider.Set(key, value);

        var cachedValue = await _provider.Get<string>(key);
        cachedValue.Should().Be(value);
    }

    [Fact]
    public async Task CacheExpiration_ShouldWork()
    {
        const string key = "expiring-key";
        const string value = "expiring-value";
        const int shortCacheTime = 1;

        await _provider.Set(key, value, shortCacheTime);

        var immediateResult = await _provider.Get<string>(key);
        immediateResult.Should().Be(value);

        await Task.Delay(1100);

        await _provider.Get<string>(key);
    }

    [Fact]
    public async Task Set_WithComplexObject_ShouldSerializeAndDeserializeCorrectly()
    {
        const string key = "complex-object-key";
        var complexObject = new TestComplexObject
        {
            Id = 123,
            Name = "Test Object",
            CreatedAt = DateTime.UtcNow,
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };

        await _provider.Set(key, complexObject);

        var cachedObject = await _provider.Get<TestComplexObject>(key);
        cachedObject.Should().NotBeNull();
        cachedObject.Should().BeEquivalentTo(complexObject);
    }

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
