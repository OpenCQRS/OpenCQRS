namespace OpenCqrs.Queries
{
    public abstract class CacheableQuery<TResult> : Query<TResult>, ICacheableQuery<TResult>
    {
        /// <inheritdoc />
        public string CacheKey { get; set; }

        /// <inheritdoc />
        public int? CacheTime { get; set; }
    }
}
