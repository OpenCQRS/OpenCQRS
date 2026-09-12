using Memoria.Web.Samples.Data;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Samples.Seeding;

/// <summary>
/// A relational store, which holds both models and is reached through a context each.
/// </summary>
/// <remarks>
/// Both contexts are taken rather than one, because the two stores share nothing: separate base
/// classes, separate tables, and neither reads the other's. Installing and emptying are therefore
/// asked of each in turn.
/// </remarks>
public sealed class RelationalSampleStore(StreamedStoreDbContext streamed, DcbStoreDbContext dcb)
    : ISampleStore
{
    /// <inheritdoc />
    /// <remarks>
    /// The database, as the connection itself reports it rather than as the connection string spells
    /// it — which is the one form that is the same across the three providers.
    /// </remarks>
    public string Where => streamed.Database.GetDbConnection().Database;

    /// <inheritdoc />
    public SampleDataScope Holds => SampleDataScope.Both;

    /// <inheritdoc />
    /// <remarks>
    /// Both stores are installed whichever half is about to be written, so that a deletion has
    /// something to delete from. Ored rather than short-circuited: the second call has to run.
    /// </remarks>
    public async Task<bool> Install(CancellationToken cancellationToken = default) =>
        await StoreSchema.Ensure(streamed, "DomainAggregates") |
        await StoreSchema.Ensure(dcb, "DcbSnapshots");

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string Table, int Rows)>> Erase(
        SampleDataScope scope, CancellationToken cancellationToken = default)
    {
        var deleted = new List<(string Table, int Rows)>();

        if (scope.HasFlag(SampleDataScope.Streamed))
        {
            deleted.AddRange(await SampleDataEraser.EraseStreamed(streamed, cancellationToken));
        }

        if (scope.HasFlag(SampleDataScope.Dcb))
        {
            deleted.AddRange(await SampleDataEraser.EraseDcb(dcb, cancellationToken));
        }

        return deleted;
    }
}
