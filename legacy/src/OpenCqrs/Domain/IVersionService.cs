using System;

namespace OpenCqrs.Domain
{
    public interface IVersionService
    {
        /// <summary>
        /// Gets the next version.
        /// </summary>
        /// <param name="aggregateRootId">The aggregate root identifier.</param>
        /// <param name="currentVersion">The current version.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <returns></returns>
        int GetNextVersion(Guid aggregateRootId, int currentVersion, int? expectedVersion);
    }
}
