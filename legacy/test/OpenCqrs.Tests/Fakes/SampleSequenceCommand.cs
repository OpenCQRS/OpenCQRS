using OpenCqrs.Commands;

namespace OpenCqrs.Tests.Fakes
{
    public class SampleCommandSequence : CommandSequence
    {
        public SampleCommandSequence()
        {
            AddCommand(new CommandInSequence());
        }
    }
}
