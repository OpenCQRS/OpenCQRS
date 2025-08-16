using System;
using System.Collections.Generic;
using OpenCqrs.Domain;

namespace OpenCqrs.Commands
{
    /// <inheritdoc />
    public partial class CommandSender : ICommandSender
    {
        /// <inheritdoc />
        public void Send<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            Process(command, () => GetCommandResponse(command));
        }

        /// <inheritdoc />
        public void Send<TCommand>(TCommand command, Func<CommandResponse> commandHandler)
            where TCommand : ICommand
        {
            Process(command, commandHandler);
        }

        /// <inheritdoc />
        public void Send(ICommandSequence commandSequence)
        {
            ProcessSequenceCommand(commandSequence);
        }

        /// <inheritdoc />
        public TResult Send<TResult>(ICommand command)
        {
            var concreteCommand = _objectFactory.CreateConcreteObject(command);
            var response = Process(command, () => GetCommandResponse(concreteCommand));
            return response?.Result != null ? (TResult)response.Result : default;
        }

        /// <inheritdoc />
        public TResult Send<TResult>(ICommand command, Func<CommandResponse> commandHandler)
        {
            var response = Process(command, commandHandler);
            return response?.Result != null ? (TResult)response.Result : default;
        }

        /// <inheritdoc />
        public TResult Send<TResult>(ICommandSequence commandSequence)
        {
            var lastStepReponse = ProcessSequenceCommand(commandSequence);
            return lastStepReponse?.Result != null ? (TResult)lastStepReponse.Result : default;
        }

        private CommandResponse ProcessSequenceCommand(ICommandSequence commandSequence)
        {
            CommandResponse lastStepResponse = null;

            foreach (var command in commandSequence.Commands)
            {
                var concreteCommand = _objectFactory.CreateConcreteObject(command);
                var response = Process(command, () => GetSequenceCommandResponse(concreteCommand, lastStepResponse));
                lastStepResponse = response;
            }

            return lastStepResponse;
        }

        private CommandResponse Process(ICommand command, Func<CommandResponse> getResponse)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (ValidateCommand(command))
            {
                var concreteCommand = _objectFactory.CreateConcreteObject(command);
                _validationService.Validate(concreteCommand);
            }

            var response = getResponse();

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

                _storeProvider.Save(new SaveStoreData
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
                    _eventPublisher.Publish(concreteEvent);
                }
            }

            return response;
        }

        private CommandResponse GetCommandResponse<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            var handler = _handlerResolver.ResolveHandler<ICommandHandler<TCommand>>();
            return handler.Handle(command);
        }

        private CommandResponse GetSequenceCommandResponse<TCommand>(TCommand command, CommandResponse previousStepResponse)
            where TCommand : ICommand
        {
            var handler = _handlerResolver.ResolveHandler<ISequenceCommandHandler<TCommand>>();
            return handler.Handle(command, previousStepResponse);
        }
    }
}
