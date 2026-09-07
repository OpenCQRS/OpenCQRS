using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads one stored aggregate out of the snapshots table.
/// </summary>
/// <remarks>
/// Deliberately not through <c>IDcbDomainService</c>. That reads through a generic method closed
/// over an aggregate type not known until someone uploads one, and it answers with the model rather
/// than the row — so the reflection was unavoidable and the row's own account of itself, its stored
/// version and the dates it was written, was lost on the way out.
/// <para>
/// Reading the row directly costs no reflection over generics at all: the payload is deserialised
/// against the runtime type in hand. It also fixes what the page means. A domain-service read folds
/// events appended since the snapshot, so the state shown could be newer than anything stored;
/// this shows what is in the table, which is what the list this page is reached from also shows.
/// Events appended since are visible as the version here trailing the boundary's own.
/// </para>
/// </remarks>
public static class AggregateReader
{
    /// <summary>
    /// Loads the aggregate stored under an exact boundary.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="aggregate">The aggregate type the payload was written from.</param>
    /// <param name="modelType">The aggregate's binding key, as <c>name:version</c>.</param>
    /// <param name="boundary">The boundary in canonical form, as <c>TagQuery.ToString</c> writes it.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// What the row holds. Everything null when there is no row: a snapshot is only ever a valid
    /// answer for the boundary that produced it, so an exact match is the same test the store
    /// itself applies, and nothing matching means nothing has been stored under it.
    /// </returns>
    public static async Task<LoadedAggregate> Load(
        IDcbDbContext context,
        Type aggregate,
        string modelType,
        string boundary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await context.DcbSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.SnapshotKind == DcbSnapshotEntity.AggregateKind &&
                           row.ModelType == modelType &&
                           row.TagQuery == boundary,
                    cancellationToken);

            return snapshot is null ? new LoadedAggregate(null, null, null) : Read(aggregate, snapshot);
        }
        catch (Exception exception)
        {
            return new LoadedAggregate(null, null, exception.Message);
        }
    }

    /// <summary>
    /// Turns one stored row into the aggregate it was written from.
    /// </summary>
    /// <param name="aggregate">The aggregate type the payload was written from.</param>
    /// <param name="snapshot">The stored row.</param>
    /// <remarks>
    /// The version and the dates come off the row rather than out of the payload, because they are
    /// the store's account of the write and not the model's own state. They survive a payload that
    /// cannot be read: when something was stored is still a fact, even when what was stored is no
    /// longer legible.
    /// </remarks>
    public static LoadedAggregate Read(Type aggregate, DcbSnapshotEntity snapshot)
    {
        var stored = new StoredSnapshot(
            snapshot.ModelType,
            snapshot.Version,
            snapshot.LatestPosition,
            snapshot.CreatedDate,
            snapshot.CreatedBy,
            snapshot.UpdatedDate,
            snapshot.UpdatedBy);

        try
        {
            // Written by DomainSerializer and only readable by it — the store's own reads go through
            // the same one, which is why a payload it wrote round-trips and a hand-edited row may not.
            var model = DomainSerializer.Current.Deserialize(snapshot.Data, aggregate);

            return model is null
                ? new LoadedAggregate(null, stored, "The stored payload is empty.")
                : new LoadedAggregate(model, stored, null);
        }
        catch (Exception exception)
        {
            return new LoadedAggregate(null, stored, exception.Message);
        }
    }
}

/// <summary>The outcome of a load.</summary>
/// <param name="Aggregate">The aggregate, or null when there is no row or its payload was unreadable.</param>
/// <param name="Snapshot">What the row says about itself, or null when there is no row.</param>
/// <param name="Error">Why the row could not be turned into an aggregate, or null when it was.</param>
public sealed record LoadedAggregate(object? Aggregate, StoredSnapshot? Snapshot, string? Error);

/// <summary>The store's account of one write, as opposed to the state that was written.</summary>
/// <param name="ModelType">The binding key the payload was stored under, as <c>name:version</c>.</param>
/// <param name="Version">The version the row was stored at.</param>
/// <param name="LatestPosition">The global position in the log the fold reached.</param>
/// <param name="Created">When it was first stored.</param>
/// <param name="CreatedBy">Who first stored it, or null when the store attributes nothing.</param>
/// <param name="Updated">When it was last stored.</param>
/// <param name="UpdatedBy">Who last stored it, or null when the store attributes nothing.</param>
public sealed record StoredSnapshot(
    string ModelType,
    int Version,
    long LatestPosition,
    DateTimeOffset Created,
    string? CreatedBy,
    DateTimeOffset Updated,
    string? UpdatedBy);
