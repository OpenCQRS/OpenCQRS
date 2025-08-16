using System;

namespace OpenCqrs.Commands
{
    public abstract class Command : ICommand
    {
        public string UserId { get; set; }
        public string Source { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public bool? ValidateCommand { get; set; }
        public bool? PublishEvents { get; set; }       
    }
}
