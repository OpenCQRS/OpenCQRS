using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using OpenCqrs.Commands;
using OpenCqrs.Configuration;
using OpenCqrs.Dependencies;
using OpenCqrs.Domain;
using OpenCqrs.Events;
using OpenCqrs.Mapping;
using OpenCqrs.Tests.Fakes;
using OpenCqrs.Validation;

namespace OpenCqrs.Tests.Commands
{
    [TestFixture]
    public class CommandSenderAsyncTests
    {
        private ICommandSender _sut;

        private Mock<IHandlerResolver> _handlerResolver;
        private Mock<IEventPublisher> _eventPublisher;
        private Mock<IStoreProvider> _storeProvider;
        private Mock<IObjectFactory> _objectFactory;
        private Mock<IValidationService> _validationService;

        private Mock<ICommandHandlerAsync<CreateSomething>> _commandHandlerAsync;
        private Mock<ICommandHandlerAsync<CreateAggregate>> _domainCommandHandlerAsync;
        private Mock<ISequenceCommandHandlerAsync<CommandInSequence>> _sequenceCommandHandlerAsync;
        private Mock<IOptions<MainOptions>> _mainOptionsMock;
        private Mock<IOptions<ValidationOptions>> _validationOptionsMock;

        private CreateSomething _createSomething;
        private CreateSomething _createSomethingConcrete;
        private SomethingCreated _somethingCreated;
        private SomethingCreated _somethingCreatedConcrete;
        private IEnumerable<IEvent> _events;

        private CreateAggregate _createAggregate;
        private CreateAggregate _createAggregateConcrete;
        private AggregateCreated _aggregateCreated;
        private AggregateCreated _aggregateCreatedConcrete;
        private Aggregate _aggregate;

        private SampleCommandSequence _sampleCommandSequence;
        private CommandInSequence _commandInSequenceConcrete;

        private CommandResponse _commandResponse;
        private CommandResponse _domainCommandResponse;

        private SaveStoreData _storeDataSaved;

        [SetUp]
        public void SetUp()
        {
            _createSomething = new CreateSomething();
            _createSomethingConcrete = new CreateSomething();
            _somethingCreated = new SomethingCreated();
            _somethingCreatedConcrete = new SomethingCreated();
            _events = new List<IEvent> { _somethingCreated };

            _createAggregate = new CreateAggregate();
            _createAggregateConcrete = new CreateAggregate();
            _aggregateCreatedConcrete = new AggregateCreated();
            _aggregate = new Aggregate();
            _aggregateCreated = (AggregateCreated)_aggregate.Events[0];

            _sampleCommandSequence = new SampleCommandSequence();
            _commandInSequenceConcrete = new CommandInSequence();

            _commandResponse = new CommandResponse { Events = _events, Result = "Result" };
            _domainCommandResponse = new CommandResponse { Events = _aggregate.Events, Result = "Result" };

            _eventPublisher = new Mock<IEventPublisher>();
            _eventPublisher
                .Setup(x => x.PublishAsync(_aggregateCreatedConcrete ))
                .Returns(Task.CompletedTask);

            _storeProvider = new Mock<IStoreProvider>();
            _storeProvider
                .Setup(x => x.SaveAsync(It.IsAny<SaveStoreData>()))
                .Callback<SaveStoreData>(x => _storeDataSaved = x)
                .Returns(Task.CompletedTask);

            _objectFactory = new Mock<IObjectFactory>();
            _objectFactory
                .Setup(x => x.CreateConcreteObject(_somethingCreated))
                .Returns(_somethingCreatedConcrete);
            _objectFactory
                .Setup(x => x.CreateConcreteObject(_aggregateCreated))
                .Returns(_aggregateCreatedConcrete);
            _objectFactory
                .Setup(x => x.CreateConcreteObject(_createSomething))
                .Returns(_createSomethingConcrete);
            _objectFactory
                .Setup(x => x.CreateConcreteObject(_createAggregate))
                .Returns(_createAggregateConcrete);
            _objectFactory
                .Setup(x => x.CreateConcreteObject(It.IsAny<CommandInSequence>()))
                .Returns(_commandInSequenceConcrete);

            _validationService = new Mock<IValidationService>();
            _validationService
                .Setup(x => x.ValidateAsync(It.IsAny<CreateSomething>()))
                .Returns(Task.CompletedTask);

            _commandHandlerAsync = new Mock<ICommandHandlerAsync<CreateSomething>>();
            _commandHandlerAsync
                .Setup(x => x.HandleAsync(_createSomethingConcrete))
                .ReturnsAsync(_commandResponse);

            _domainCommandHandlerAsync = new Mock<ICommandHandlerAsync<CreateAggregate>>();
            _domainCommandHandlerAsync
                .Setup(x => x.HandleAsync(_createAggregate))
                .ReturnsAsync(_domainCommandResponse);
            _domainCommandHandlerAsync
                .Setup(x => x.HandleAsync(_createAggregateConcrete))
                .ReturnsAsync(_domainCommandResponse);

            _sequenceCommandHandlerAsync = new Mock<ISequenceCommandHandlerAsync<CommandInSequence>>();
            _sequenceCommandHandlerAsync
                .Setup(x => x.HandleAsync(It.IsAny<CommandInSequence>(), It.IsAny<CommandResponse>()))
                .ReturnsAsync(_commandResponse);

            _handlerResolver = new Mock<IHandlerResolver>();
            _handlerResolver
                .Setup(x => x.ResolveHandler<ICommandHandlerAsync<CreateSomething>>())
                .Returns(_commandHandlerAsync.Object);
            _handlerResolver
                .Setup(x => x.ResolveHandler<ICommandHandlerAsync<CreateAggregate>>())
                .Returns(_domainCommandHandlerAsync.Object);
            _handlerResolver
                .Setup(x => x.ResolveHandler<ISequenceCommandHandlerAsync<CommandInSequence>>())
                .Returns(_sequenceCommandHandlerAsync.Object);

            _mainOptionsMock = new Mock<IOptions<MainOptions>>();
            _mainOptionsMock
                .Setup(x => x.Value)
                .Returns(new MainOptions());            
            
            _validationOptionsMock = new Mock<IOptions<ValidationOptions>>();
            _validationOptionsMock
                .Setup(x => x.Value)
                .Returns(new ValidationOptions());

            _sut = new CommandSender(_handlerResolver.Object,
                _eventPublisher.Object,
                _objectFactory.Object,
                _storeProvider.Object,
                _validationService.Object,
                _mainOptionsMock.Object,
                _validationOptionsMock.Object);
        }

        [Test]
        public void SendAsync_ThrowsException_WhenCommandIsNull()
        {
            _createAggregate = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.SendAsync(_createAggregate));
        }

        [Test]
        public async Task SendAsync_ValidatesCommand()
        {
            _createSomething.ValidateCommand = true;
            await _sut.SendAsync(_createSomething);
            _validationService.Verify(x => x.ValidateAsync(It.IsAny<CreateSomething>()), Times.Once);
        }

        [Test]
        public async Task SendAsync_HandlesCommand()
        {
            await _sut.SendAsync(_createSomething);
            _commandHandlerAsync.Verify(x => x.HandleAsync(_createSomething), Times.Once);
        }

        [Test]
        public async Task SendAsync_HandlesDomainCommand()
        {
            await _sut.SendAsync(_createAggregate);
            _domainCommandHandlerAsync.Verify(x => x.HandleAsync(_createAggregate), Times.Once);
        }

        [Test]
        public async Task SendAsync_HandlesCommand_InCommandSequence()
        {
            await _sut.SendAsync(_sampleCommandSequence);
            _sequenceCommandHandlerAsync.Verify(x => x.HandleAsync(It.IsAny<CommandInSequence>(), It.IsAny<CommandResponse>()), Times.Once);
        }

        [Test]
        public async Task SendAsync_SavesStoreData()
        {
            await _sut.SendAsync(_createAggregate);
            _storeProvider.Verify(x => x.SaveAsync(It.IsAny<SaveStoreData>()), Times.Once);
        }

        [Test]
        public async Task SendAsync_SavesCorrectData()
        {
            await _sut.SendAsync(_createAggregate);
            Assert.AreEqual(_aggregate.GetType(), _storeDataSaved.AggregateType);
            Assert.AreEqual(_createAggregate.AggregateRootId, _storeDataSaved.AggregateRootId);
            Assert.AreEqual(_aggregateCreated, _storeDataSaved.Events.FirstOrDefault());
            Assert.AreEqual(_createAggregate, _storeDataSaved.DomainCommand);
        }

        [Test]
        public async Task SendAsync_PublishesEvents()
        {
            await _sut.SendAsync(_createAggregate);
            _eventPublisher.Verify(x => x.PublishAsync(_aggregateCreatedConcrete ), Times.Once);
        }

        [Test]
        public async Task SendAsync_NotPublishesEvents_WhenSetInOptions()
        {
            _mainOptionsMock
                .Setup(x => x.Value)
                .Returns(new MainOptions { PublishEvents = false });

            _sut = new CommandSender(_handlerResolver.Object,
                _eventPublisher.Object,
                _objectFactory.Object,
                _storeProvider.Object,
                new Mock<IValidationService>().Object,
                _mainOptionsMock.Object,
                _validationOptionsMock.Object);

            await _sut.SendAsync(_createAggregate);
            _eventPublisher.Verify(x => x.PublishAsync(_aggregateCreatedConcrete), Times.Never);
        }

        [Test]
        public async Task SendAsync_NotPublishesEvents_WhenSetInCommand()
        {
            _createAggregate.PublishEvents = false;
            await _sut.SendAsync(_createAggregate);
            _eventPublisher.Verify(x => x.PublishAsync(_aggregateCreatedConcrete), Times.Never);
        }

        [Test]
        public async Task SendAsyncWithResult_ReturnsResult()
        {
            var actual = await _sut.SendAsync<string>(_createSomething);
            Assert.AreEqual("Result", actual);
        }
    }
}
