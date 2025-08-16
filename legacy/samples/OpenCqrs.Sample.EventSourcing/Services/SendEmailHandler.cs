using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCqrs.Commands;
using OpenCqrs.Events;

namespace OpenCqrs.Sample.EventSourcing.Services
{
    public class SendEmailHandler : ICommandHandlerAsync<SendEmail>
    {
        public Task<CommandResponse> HandleAsync(SendEmail command)
        {
            return Task.FromResult(new CommandResponse
            { 
                Events = new List<IEvent>()
                {
                    new EmailSent()
                }
            });
        }
    }
}
