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
    public class EventPublisherTests
    {
        private IEventPublisher _sut;

        private Mock<IResolver> _resolver;
        private Mock<IBusMessageDispatcher> _busMessageDispatcher;

        private Mock<IEventHandler<SomethingCreated>> _eventHandler1;
        private Mock<IEventHandler<SomethingCreated>> _eventHandler2;

        private SomethingCreated _somethingCreated;

        [SetUp]
        public void SetUp()
        {
            _somethingCreated = new SomethingCreated();

            _eventHandler1 = new Mock<IEventHandler<SomethingCreated>>();
            _eventHandler1
                .Setup(x => x.Handle(_somethingCreated));

            _eventHandler2 = new Mock<IEventHandler<SomethingCreated>>();
            _eventHandler2
                .Setup(x => x.Handle(_somethingCreated));

            _resolver = new Mock<IResolver>();
            _resolver
                .Setup(x => x.ResolveAll<IEventHandler<SomethingCreated>>())
                .Returns(new List<IEventHandler<SomethingCreated>>{_eventHandler1.Object, _eventHandler2.Object});

            _busMessageDispatcher = new Mock<IBusMessageDispatcher>();
            _busMessageDispatcher
                .Setup(x => x.DispatchAsync(_somethingCreated))
                .Returns(Task.CompletedTask);

            _sut = new EventPublisher(_resolver.Object, _busMessageDispatcher.Object);
        }
    
        [Test]
        public void Publish_ThrowsException_WhenEventIsNull()
        {
            _somethingCreated = null;
            Assert.Throws<ArgumentNullException>(() => _sut.Publish(_somethingCreated));
        }

        [Test]
        public void Publish_PublishesFirstEvent()
        {
            _sut.Publish(_somethingCreated);
            _eventHandler1.Verify(x => x.Handle(_somethingCreated), Times.Once);
        }

        [Test]
        public void Publish_PublishesSecondEvent()
        {
            _sut.Publish(_somethingCreated);
            _eventHandler2.Verify(x => x.Handle(_somethingCreated), Times.Once);
        }

        [Test]
        public void Publish_DispatchesEventToServiceBus()
        {
            _sut.Publish(_somethingCreated);
            _busMessageDispatcher.Verify(x => x.DispatchAsync(It.IsAny<IBusMessage>()), Times.Once);
        }
    }
}
