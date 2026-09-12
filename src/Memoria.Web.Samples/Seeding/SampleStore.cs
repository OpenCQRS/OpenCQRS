namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// The store a run was pointed at, in the three terms a run needs it in.
/// </summary>
/// <remarks>
/// <para>
/// A relational store and a Cosmos store are not variations of one another. One is written through
/// contexts Entity Framework Core opens, and installing it means creating tables; the other has no
/// context at all, is written through the SDK-based domain service, and installing it means creating
/// a database and a container. Nothing above this cares which: it asks where the run is pointed, has
/// the store installed, and asks for it to be emptied.
/// </para>
/// <para>
/// The seeding itself is not here, because it does not need to be: it writes through
/// <c>IDomainService</c> and <c>IDcbDomainService</c>, which every store registers. Only what
/// the framework has no operation for — provisioning and deletion — differs by engine.
/// </para>
/// </remarks>
public interface ISampleStore
{
    /// <summary>
    /// Where the run is pointed, as a line to print. Enough to recognise the store by, since the
    /// first thing anyone doubts is whether the seeding went where they meant it to.
    /// </summary>
    string Where { get; }

    /// <summary>
    /// Which sample data this store can hold.
    /// </summary>
    /// <remarks>
    /// A relational store holds both models. A Cosmos store holds only the streamed one: there is no
    /// Cosmos dynamic consistency boundary store, so a run is never offered that half rather than
    /// offered it and refused on the first write.
    /// </remarks>
    SampleDataScope Holds { get; }

    /// <summary>
    /// Creates whatever the store needs before anything writes to it.
    /// </summary>
    /// <returns>Whether anything was created.</returns>
    Task<bool> Install(CancellationToken cancellationToken = default);

    /// <summary>
    /// Empties the store of the data in scope, so a run can start from nothing.
    /// </summary>
    /// <returns>How much went, and what it was counted in.</returns>
    Task<IReadOnlyList<(string Table, int Rows)>> Erase(
        SampleDataScope scope, CancellationToken cancellationToken = default);
}
