using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OpenCqrs.Bus;
using OpenCqrs.Commands;
using OpenCqrs.Events;
using OpenCqrs.Queries;
using OpenCqrs.Tests.Fakes;

namespace OpenCqrs.Tests
{
    [TestFixture]
    public class DispatcherTests
    {
        private IDispatcher _sut;

        private Mock<ICommandSender> _commandSender;
        private Mock<IEventPublisher> _eventPublisher;
        private Mock<IQueryProcessor> _queryDispatcher;
        private Mock<IBusMessageDispatcher> _busMessageDispatcher;

        private SomethingCreated _somethingCreated;
        private GetSomething _getSomething;
        private Something _something;
        private CreateAggregate _createAggregate;
        private CreateAggregateBusMessage _createAggregateBusMessage;
        private SampleCommandSequence _sampleCommandSequence;

        [SetUp]
        public void SetUp()
        {
            _somethingCreated = new SomethingCreated();
            _getSomething = new GetSomething();
            _something = new Something();
            _createAggregate = new CreateAggregate();
            _createAggregateBusMessage = new CreateAggregateBusMessage();
            _sampleCommandSequence = new SampleCommandSequence();

            _commandSender = new Mock<ICommandSender>();
            _commandSender
                .Setup(x => x.SendAsync(_createAggregate))
                .Returns(Task.CompletedTask);
            _commandSender
                .Setup(x => x.Send(_createAggregate));
            _commandSender
                .Setup(x => x.SendAsync(_sampleCommandSequence))
                .Returns(Task.CompletedTask);
            _commandSender
                .Setup(x => x.Send(_sampleCommandSequence));

            _eventPublisher = new Mock<IEventPublisher>();
            _eventPublisher
                .Setup(x => x.PublishAsync(_somethingCreated))
                .Returns(Task.CompletedTask);
            _eventPublisher
                .Setup(x => x.Publish(_somethingCreated));

            _queryDispatcher = new Mock<IQueryProcessor>();
            _queryDispatcher
                .Setup(x => x.ProcessAsync(_getSomething))
                .ReturnsAsync(_something);
            _queryDispatcher
                .Setup(x => x.Process(_getSomething))
                .Returns(_something);

            _busMessageDispatcher = new Mock<IBusMessageDispatcher>();
            _busMessageDispatcher
                .Setup(x => x.DispatchAsync(_createAggregateBusMessage))
                .Returns(Task.CompletedTask);

            _sut = new Dispatcher(_commandSender.Object,
                _eventPublisher.Object,
                _queryDispatcher.Object,
                _busMessageDispatcher.Object);
        }

        [Test]
        public async Task SendsCommandAsync()
        {
            await _sut.SendAsync(_createAggregate);
            _commandSender.Verify(x => x.SendAsync(_createAggregate), Times.Once);
        }

        [Test]
        public async Task SendsCommandWithResultAsync()
        {
            await _sut.SendAsync<string>(_createAggregate);
            _commandSender.Verify(x => x.SendAsync<string>(_createAggregate), Times.Once);
        }

        [Test]
        public async Task SendsCommandSequenceAsync()
        {
            await _sut.SendAsync(_sampleCommandSequence);
            _commandSender.Verify(x => x.SendAsync(_sampleCommandSequence), Times.Once);
        }

        [Test]
        public async Task SendsCommandSequenceWithResultAsync()
        {
            await _sut.SendAsync<string>(_sampleCommandSequence);
            _commandSender.Verify(x => x.SendAsync<string>(_sampleCommandSequence), Times.Once);
        }

        [Test]
        public void SendsCommand()
        {
            _sut.Send(_createAggregate);
            _commandSender.Verify(x => x.Send(_createAggregate), Times.Once);
        }

        [Test]
        public void SendsCommandWithResult()
        {
            _sut.Send<string>(_createAggregate);
            _commandSender.Verify(x => x.Send<string>(_createAggregate), Times.Once);
        }

        [Test]
        public void SendsCommandSequence()
        {
            _sut.Send(_sampleCommandSequence);
            _commandSender.Verify(x => x.Send(_sampleCommandSequence), Times.Once);
        }

        [Test]
        public void SendsCommandSequenceWithResult()
        {
            _sut.Send<string>(_sampleCommandSequence);
            _commandSender.Verify(x => x.Send<string>(_sampleCommandSequence), Times.Once);
        }

        [Test]
        public async Task PublishesEventAsync()
        {
            await _sut.PublishAsync(_somethingCreated);
            _eventPublisher.Verify(x => x.PublishAsync(_somethingCreated), Times.Once);
        }

        [Test]
        public void PublishesEvent()
        {
            _sut.Publish(_somethingCreated);
            _eventPublisher.Verify(x => x.Publish(_somethingCreated), Times.Once);
        }

        [Test]
        public async Task GetsResultAsync()
        {
            await _sut.GetResultAsync(_getSomething);
            _queryDispatcher.Verify(x => x.ProcessAsync(_getSomething), Times.Once);
        }

        [Test]
        public void GetsResult()
        {
            _sut.GetResult(_getSomething);
            _queryDispatcher.Verify(x => x.Process(_getSomething), Times.Once);
        }

        [Test]
        public async Task DispatchesBusMessageAsync()
        {
            await _sut.DispatchBusMessageAsync(_createAggregateBusMessage);
            _busMessageDispatcher.Verify(x => x.DispatchAsync(_createAggregateBusMessage), Times.Once);
        }
    }
}
