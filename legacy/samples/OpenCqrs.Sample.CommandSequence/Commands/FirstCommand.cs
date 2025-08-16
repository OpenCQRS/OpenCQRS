using OpenCqrs.Commands;

namespace OpenCqrs.Sample.CommandSequence.Commands
{
    public class FirstCommand : Command
    {
        public string Name { get; set; }

        public FirstCommand()
        {
            ValidateCommand = true;
        }
    }
}
