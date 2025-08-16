using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using OpenCqrs.Caching;
using OpenCqrs.Dependencies;
using OpenCqrs.Queries;
using OpenCqrs.Tests.Fakes;

namespace OpenCqrs.Tests.Queries
{
    [TestFixture]
    public class QueryProcessorAsyncTests
    {
        private IQueryProcessor _sut;

        private Mock<IHandlerResolver> _handlerResolver;
        private Mock<ICacheManager> _cacheManager;
        private Mock<IOptions<CacheOptions>> _cacheOptions;
        private Mock<IQueryHandlerAsync<GetSomething, Something>> _queryHandler;

        private GetSomething _getSomething;
        private GetSomethingCacheable _getSomethingCacheable;
        private Something _something;

        [SetUp]
        public void SetUp()
        {
            _getSomething = new GetSomething();
            _getSomethingCacheable = new GetSomethingCacheable();
            _something = new Something();

            _queryHandler = new Mock<IQueryHandlerAsync<GetSomething, Something>>();
            _queryHandler
                .Setup(x => x.HandleAsync(_getSomething))
                .ReturnsAsync(_something);

            _handlerResolver = new Mock<IHandlerResolver>();
            _handlerResolver
                .Setup(x => x.ResolveHandler<IQueryHandlerAsync<GetSomething, Something>>())
                .Returns(_queryHandler.Object);

            _cacheManager = new Mock<ICacheManager>();
            _cacheManager
                .Setup(x => x.GetOrSetAsync(_getSomethingCacheable.CacheKey, It.IsAny<int>(), It.IsAny<Func<Task<Something>>>()))
                .ReturnsAsync(_something);

            _cacheOptions = new Mock<IOptions<CacheOptions>>();
            _cacheOptions
                .Setup(x => x.Value)
                .Returns(new CacheOptions());

            _sut = new QueryProcessor(_handlerResolver.Object, _cacheManager.Object, _cacheOptions.Object);
        }
    
        [Test]
        public void ProcessAsync_ThrowsException_WhenQueryIsNull()
        {
            _getSomething = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.ProcessAsync(_getSomething));
        }

        [Test]
        public async Task ProcessAsync_ReturnResult()
        {
            var result = await _sut.ProcessAsync(_getSomething);
            Assert.AreEqual(_something, result);
        }

        [Test]
        public async Task ProcessAsync_ReturnsResultFromCache()
        {
            var result = await _sut.ProcessAsync(_getSomethingCacheable);
            _cacheManager.Verify(x => x.GetOrSetAsync(_getSomethingCacheable.CacheKey, It.IsAny<int>(), It.IsAny<Func<Task<Something>>>()), Times.Once);
        }
    }
}
