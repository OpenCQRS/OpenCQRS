using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OpenCqrs.Bus;
using OpenCqrs.Dependencies;
using OpenCqrs.Events;
using OpenCqrs.Tests.Fakes;

namespace OpenCqrs.Tests.Events
{
    [TestFixture]
    public class EventPublisherAsyncTests
    {
        private IEventPublisher _sut;

        private Mock<IResolver> _resolver;
        private Mock<IBusMessageDispatcher> _busMessageDispatcher;

        private Mock<IEventHandlerAsync<SomethingCreated>> _eventHandler1;
        private Mock<IEventHandlerAsync<SomethingCreated>> _eventHandler2;

        private SomethingCreated _somethingCreated;

        [SetUp]
        public void SetUp()
        {
            _somethingCreated = new SomethingCreated();

            _eventHandler1 = new Mock<IEventHandlerAsync<SomethingCreated>>();
            _eventHandler1
                .Setup(x => x.HandleAsync(_somethingCreated))
                .Returns(Task.CompletedTask);

            _eventHandler2 = new Mock<IEventHandlerAsync<SomethingCreated>>();
            _eventHandler2
                .Setup(x => x.HandleAsync(_somethingCreated))
                .Returns(Task.CompletedTask);

            _resolver = new Mock<IResolver>();
            _resolver
                .Setup(x => x.ResolveAll<IEventHandlerAsync<SomethingCreated>>())
                .Returns(new List<IEventHandlerAsync<SomethingCreated>>{_eventHandler1.Object, _eventHandler2.Object});

            _busMessageDispatcher = new Mock<IBusMessageDispatcher>();
            _busMessageDispatcher
                .Setup(x => x.DispatchAsync(_somethingCreated))
                .Returns(Task.CompletedTask);

            _sut = new EventPublisher(_resolver.Object, _busMessageDispatcher.Object);
        }
    
        [Test]
        public void PublishAsync_ThrowsException_WhenEventIsNull()
        {
            _somethingCreated = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.PublishAsync(_somethingCreated));
        }

        [Test]
        public async Task PublishAsync_PublishesFirstEvent()
        {
            await _sut.PublishAsync(_somethingCreated);
            _eventHandler1.Verify(x => x.HandleAsync(_somethingCreated), Times.Once);
        }

        [Test]
        public async Task PublishAsync_PublishesSecondEvent()
        {
            await _sut.PublishAsync(_somethingCreated);
            _eventHandler2.Verify(x => x.HandleAsync(_somethingCreated), Times.Once);
        }

        [Test]
        public async Task PublishAsync_DispatchesEventToServiceBus()
        {
            await _sut.PublishAsync(_somethingCreated);
            _busMessageDispatcher.Verify(x => x.DispatchAsync(It.IsAny<IBusMessage>()), Times.Once);
        }
    }
}
