namespace OpenCqrs.Sample.CommandSequence.Commands
{
    public class SampleCommandSequence : OpenCqrs.Commands.CommandSequence
    {
        public SampleCommandSequence()
        {
            AddCommand(new FirstCommand { Name = "My Name" });
            AddCommand(new SecondCommand());
            AddCommand(new ThirdCommand());
        }
    }
}
