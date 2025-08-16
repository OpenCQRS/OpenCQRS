namespace OpenCqrs.Configuration
{
    public class MainOptions
    {
        /// <summary>
        /// The value indicating whether events are published automatically. Default value is true.
        /// </summary>
        public bool PublishEvents { get; set; } = true;

        /// <summary>
        /// The value indicating whether commands are saved alongside events. Default value is true.
        /// </summary>
        public bool SaveCommandData { get; set; } = true;
    }
}
