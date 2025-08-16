using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CommandSenderSyncTests
    {
        private ICommandSender _sut;

        private Mock<IHandlerResolver> _handlerResolver;
        private Mock<IEventPublisher> _eventPublisher;
        private Mock<IStoreProvider> _storeProvider;
        private Mock<IObjectFactory> _objectFactory;
        private Mock<IValidationService> _validationService;

        private Mock<ICommandHandler<CreateSomething>> _commandHandler;
        private Mock<ICommandHandler<CreateAggregate>> _domainCommandHandler;
        private Mock<ISequenceCommandHandler<CommandInSequence>> _sequenceCommandHandler;
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
                .Setup(x => x.Publish(_aggregateCreatedConcrete));

            _storeProvider = new Mock<IStoreProvider>();
            _storeProvider
                .Setup(x => x.Save(It.IsAny<SaveStoreData>()))
                .Callback<SaveStoreData>(x => _storeDataSaved = x);

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
                .Setup(x => x.Validate(It.IsAny<CreateSomething>()));

            _commandHandler = new Mock<ICommandHandler<CreateSomething>>();
            _commandHandler
                .Setup(x => x.Handle(_createSomethingConcrete))
                .Returns(_commandResponse);

            _domainCommandHandler = new Mock<ICommandHandler<CreateAggregate>>();
            _domainCommandHandler
                .Setup(x => x.Handle(_createAggregate))
                .Returns(_domainCommandResponse);
            _domainCommandHandler
                .Setup(x => x.Handle(_createAggregateConcrete))
                .Returns(_domainCommandResponse);

            _sequenceCommandHandler = new Mock<ISequenceCommandHandler<CommandInSequence>>();
            _sequenceCommandHandler
                .Setup(x => x.Handle(It.IsAny<CommandInSequence>(), It.IsAny<CommandResponse>()))
                .Returns(_commandResponse);

            _handlerResolver = new Mock<IHandlerResolver>();
            _handlerResolver
                .Setup(x => x.ResolveHandler<ICommandHandler<CreateSomething>>())
                .Returns(_commandHandler.Object);
            _handlerResolver
                .Setup(x => x.ResolveHandler<ICommandHandler<CreateAggregate>>())
                .Returns(_domainCommandHandler.Object);
            _handlerResolver
                .Setup(x => x.ResolveHandler<ISequenceCommandHandler<CommandInSequence>>())
                .Returns(_sequenceCommandHandler.Object);

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
        public void Send_ThrowsException_WhenCommandIsNull()
        {
            _createAggregate = null;
            Assert.Throws<ArgumentNullException>(() => _sut.Send(_createAggregate));
        }

        [Test]
        public void Send_ValidatesCommand()
        {
            _createSomething.ValidateCommand = true;
            _sut.Send(_createSomething);
            _validationService.Verify(x => x.Validate(It.IsAny<CreateSomething>()), Times.Once);
        }

        [Test]
        public void Send_HandlesCommand()
        {
            _sut.Send(_createSomething);
            _commandHandler.Verify(x => x.Handle(_createSomething), Times.Once);
        }

        [Test]
        public void Send_HandlesDomainCommand()
        {
            _sut.Send(_createAggregate);
            _domainCommandHandler.Verify(x => x.Handle(_createAggregate), Times.Once);
        }

        [Test]
        public void Send_HandlesCommand_InCommandSequence()
        {
            _sut.Send(_sampleCommandSequence);
            _sequenceCommandHandler.Verify(x => x.Handle(It.IsAny<CommandInSequence>(), It.IsAny<CommandResponse>()), Times.Once);
        }

        [Test]
        public void Send_SavesStoreData()
        {
            _sut.Send(_createAggregate);
            _storeProvider.Verify(x => x.Save(It.IsAny<SaveStoreData>()), Times.Once);
        }

        [Test]
        public void Send_SavesCorrectData()
        {
            _sut.Send(_createAggregate);
            Assert.AreEqual(_aggregate.GetType(), _storeDataSaved.AggregateType);
            Assert.AreEqual(_createAggregate.AggregateRootId, _storeDataSaved.AggregateRootId);
            Assert.AreEqual(_aggregateCreated, _storeDataSaved.Events.FirstOrDefault());
            Assert.AreEqual(_createAggregate, _storeDataSaved.DomainCommand);
        }

        [Test]
        public void Send_PublishesEvents()
        {
            _sut.Send(_createAggregate);
            _eventPublisher.Verify(x => x.Publish(_aggregateCreatedConcrete), Times.Once);
        }

        [Test]
        public void Send_NotPublishesEvents_WhenSetInOptions()
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

            _sut.Send(_createAggregate);
            _eventPublisher.Verify(x => x.Publish(_aggregateCreatedConcrete), Times.Never);
        }

        [Test]
        public void Send_NotPublishesEvents_WhenSetInCommand()
        {
            _createAggregate.PublishEvents = false;
            _sut.Send(_createAggregate);
            _eventPublisher.Verify(x => x.Publish(_aggregateCreatedConcrete), Times.Never);
        }

        [Test]
        public void SendWithResult_ReturnsResult()
        {
            var actual = _sut.Send<string>(_createSomething);
            Assert.AreEqual("Result", actual);
        }
    }
}
