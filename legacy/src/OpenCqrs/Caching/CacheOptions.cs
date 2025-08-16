namespace OpenCqrs.Caching
{
    public class CacheOptions
    {
        /// <summary>
        /// The value indicating the default cache time (in seconds). 
        /// Default value is 60.
        /// </summary>
        public int DefaultCacheTime { get; set; } = 60;
    }
}
