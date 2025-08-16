using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenCqrs.Configuration;
using OpenCqrs.Domain;

namespace OpenCqrs.Store.Cosmos.Mongo.Documents.Factories
{
    public class CommandDocumentFactory : ICommandDocumentFactory
    {
        private readonly MainOptions _mainOptions;

        private bool SaveCommandData(IDomainCommand command) => command.SaveCommandData ?? _mainOptions.SaveCommandData;

        public CommandDocumentFactory(IOptions<MainOptions> mainOptions)
        {
            _mainOptions = mainOptions.Value;
        }

        public CommandDocument CreateCommand(IDomainCommand command)
        {
            return new CommandDocument
            {
                Id = command.Id.ToString(),
                AggregateId = command.AggregateRootId.ToString(),
                Type = command.GetType().AssemblyQualifiedName,
                Data = SaveCommandData(command) ? JsonConvert.SerializeObject(command) : null,
                TimeStamp = command.TimeStamp,
                UserId = command.UserId,
                Source = command.Source
            };
        }
    }
}
