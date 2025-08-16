namespace OpenCqrs.Validation
{
    public class ValidationOptions
    {
        /// <summary>
        /// The value indicating whether to validate all commands. 
        /// Default value is false.
        /// </summary>
        public bool ValidateAllCommands { get; set; } = false;
    }
}
