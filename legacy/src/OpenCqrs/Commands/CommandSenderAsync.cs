using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenCqrs.Configuration;
using OpenCqrs.Dependencies;
using OpenCqrs.Domain;
using OpenCqrs.Events;
using OpenCqrs.Mapping;
using OpenCqrs.Validation;

namespace OpenCqrs.Commands
{
    /// <inheritdoc />
    public partial class CommandSender : ICommandSender
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly IEventPublisher _eventPublisher;
        private readonly IObjectFactory _objectFactory;
        private readonly IStoreProvider _storeProvider;
        private readonly IValidationService _validationService;
        private readonly MainOptions _mainOptions;
        private readonly ValidationOptions _validationOptions;

        private bool ValidateCommand(ICommand command) => command.ValidateCommand ?? _validationOptions.ValidateAllCommands;
        private bool PublishEvents(ICommand command) => command.PublishEvents ?? _mainOptions.PublishEvents;

        public CommandSender(IHandlerResolver handlerResolver,
            IEventPublisher eventPublisher,
            IObjectFactory objectFactory,
            IStoreProvider storeProvider,
            IValidationService validationService,
            IOptions<MainOptions> mainOptions,
            IOptions<ValidationOptions> validationOptions)
        {
            _handlerResolver = handlerResolver;
            _eventPublisher = eventPublisher;
            _objectFactory = objectFactory;
            _storeProvider = storeProvider;
            _validationService = validationService;
            _mainOptions = mainOptions.Value;
            _validationOptions = validationOptions?.Value ?? new ValidationOptions();
        }

        /// <inheritdoc />
        public async Task SendAsync<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            await ProcessAsync(command, () => GetCommandResponseAsync(command));
        }

        /// <inheritdoc />
        public async Task SendAsync<TCommand>(TCommand command, Func<Task<CommandResponse>> commandHandler)
            where TCommand : ICommand
        {
            await ProcessAsync(command, commandHandler);
        }

        /// <inheritdoc />
        public Task SendAsync(ICommandSequence commandSequence)
        {
            return ProcessCommandSequenceAsync(commandSequence);
        }

        /// <inheritdoc />
        public async Task<TResult> SendAsync<TResult>(ICommand command)
        {
            var concreteCommand = _objectFactory.CreateConcreteObject(command);
            var response = await ProcessAsync(command, () => GetCommandResponseAsync(concreteCommand));
            return response?.Result != null ? (TResult)response.Result : default;
        }

        /// <inheritdoc />
        public async Task<TResult> SendAsync<TResult>(ICommand command, Func<Task<CommandResponse>> commandHandler)
        {
            var response = await ProcessAsync(command, commandHandler);
            return response?.Result != null ? (TResult)response.Result : default;
        }

        /// <inheritdoc />
        public async Task<TResult> SendAsync<TResult>(ICommandSequence commandSequence)
        {
            var lastStepResponse = await ProcessCommandSequenceAsync(commandSequence);
            return lastStepResponse?.Result != null ? (TResult)lastStepResponse.Result : default;
        }

        private async Task<CommandResponse> ProcessCommandSequenceAsync(ICommandSequence commandSequence)
        {
            CommandResponse lastStepResponse = null;

            foreach (var command in commandSequence.Commands)
            {
                var concreteCommand = _objectFactory.CreateConcreteObject(command);
                var response = await ProcessAsync(command, () => GetSequenceCommandResponseAsync(concreteCommand, lastStepResponse));
                lastStepResponse = response;
            }

            return lastStepResponse;
        }

        private async Task<CommandResponse> ProcessAsync<TCommand>(TCommand command, Func<Task<CommandResponse>> getResponse)
            where TCommand : ICommand
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (ValidateCommand(command))
            {
                var concreteCommand = _objectFactory.CreateConcreteObject(command);
                await _validationService.ValidateAsync(concreteCommand);
            }

            var response = await getResponse();

            if (response == null)
            {
                return null;
            }

            if (command is IDomainCommand domainCommand)
            {
                foreach (var @event in (IEnumerable<IDomainEvent>)response.Events)
                {
                    @event.Update(domainCommand);
                }

                await _storeProvider.SaveAsync(new SaveStoreData
                {
                    AggregateType = GetAggregateType(domainCommand),
                    AggregateRootId = domainCommand.AggregateRootId,
                    Events = (IEnumerable<IDomainEvent>)response.Events,
                    DomainCommand = domainCommand
                });
            }

            if (PublishEvents(command))
            {
                foreach (var @event in response.Events)
                {
                    var concreteEvent = _objectFactory.CreateConcreteObject(@event);
                    await _eventPublisher.PublishAsync(concreteEvent);
                }
            }

            return response;
        }

        private Task<CommandResponse> GetCommandResponseAsync<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            var handler = _handlerResolver.ResolveHandler<ICommandHandlerAsync<TCommand>>();
            return handler.HandleAsync(command);
        }

        private Task<CommandResponse> GetSequenceCommandResponseAsync<TCommand>(TCommand command, CommandResponse previousStepResponse)
            where TCommand : ICommand
        {
            var handler = _handlerResolver.ResolveHandler<ISequenceCommandHandlerAsync<TCommand>>();
            return handler.HandleAsync(command, previousStepResponse);
        }

        private static Type GetAggregateType(IDomainCommand domainCommand)
        {
            var commandType = domainCommand.GetType();
            var commandInterface = commandType.GetInterfaces()[1];
            var aggregateType = commandInterface.GetGenericArguments().FirstOrDefault();
            return aggregateType;
        }
    }
}
