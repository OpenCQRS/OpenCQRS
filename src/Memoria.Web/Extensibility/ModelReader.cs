using Memoria.EventSourcing;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Reads one stored aggregate or projection out of the snapshots table.
/// </summary>
/// <remarks>
/// One reader for both, as there is one table for both: an aggregate's snapshot and a projection's
/// differ only in the discriminator on the row, and a payload reads back into the type that wrote it
/// whichever kind that type is.
/// <para>
/// Deliberately not through <c>IDcbDomainService</c>. That reads through a generic method closed
/// over a model type not known until someone uploads one, and it answers with the model rather
/// than the row — so the reflection was unavoidable and the row's own account of itself, its stored
/// version and the dates it was written, was lost on the way out.
/// </para>
/// <para>
/// Reading the row directly costs no reflection over generics at all: the payload is deserialised
/// against the runtime type in hand. It also fixes what the page means. A domain-service read folds
/// events appended since the snapshot, so the state shown could be newer than anything stored;
/// this shows what is in the table, which is what the list this page is reached from also shows.
/// Events appended since are visible as the version here trailing the boundary's own.
/// </para>
/// </remarks>
public static class ModelReader
{
    /// <summary>
    /// Loads the model stored under an exact boundary.
    /// </summary>
    /// <param name="context">The DCB store.</param>
    /// <param name="model">The aggregate or projection type the payload was written from.</param>
    /// <param name="kind">Which of the two it is, as the row discriminates them.</param>
    /// <param name="modelType">The model's binding key, as <c>name:version</c>.</param>
    /// <param name="boundary">The boundary in canonical form, as <c>TagQuery.ToString</c> writes it.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// What the row holds. Everything null when there is no row: a snapshot is only ever a valid
    /// answer for the boundary that produced it, so an exact match is the same test the store
    /// itself applies, and nothing matching means nothing has been stored under it.
    /// </returns>
    public static async Task<LoadedModel> Load(
        IDcbDbContext context,
        Type model,
        DcbModelKind kind,
        string modelType,
        string boundary,
        CancellationToken cancellationToken = default)
    {
        var snapshotKind = kind.SnapshotKind();

        try
        {
            var snapshot = await context.DcbSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.SnapshotKind == snapshotKind &&
                           row.ModelType == modelType &&
                           row.TagQuery == boundary,
                    cancellationToken);

            return snapshot is null ? new LoadedModel(null, null, null) : Read(model, snapshot);
        }
        catch (Exception exception)
        {
            return new LoadedModel(null, null, exception.Message);
        }
    }

    /// <summary>
    /// Turns one stored row into the model it was written from.
    /// </summary>
    /// <param name="model">The aggregate or projection type the payload was written from.</param>
    /// <param name="snapshot">The stored row.</param>
    /// <remarks>
    /// The version and the dates come off the row rather than out of the payload, because they are
    /// the store's account of the write and not the model's own state. They survive a payload that
    /// cannot be read: when something was stored is still a fact, even when what was stored is no
    /// longer legible.
    /// </remarks>
    public static LoadedModel Read(Type model, DcbSnapshotEntity snapshot)
    {
        var stored = new StoredSnapshot(
            snapshot.Id,
            snapshot.StoreId,
            snapshot.ModelType,
            snapshot.Version,
            snapshot.LatestPosition,
            snapshot.CreatedDate,
            snapshot.CreatedBy,
            snapshot.UpdatedDate,
            snapshot.UpdatedBy);

        var opened = Open(model, snapshot.Data);

        return new LoadedModel(opened.Model, stored, opened.Error);
    }

    /// <summary>
    /// Turns one stored payload back into the model it was written from.
    /// </summary>
    /// <param name="model">The aggregate or projection type the payload was written from.</param>
    /// <param name="data">The payload, as the store wrote it.</param>
    /// <returns>The model, or why it could not be read back.</returns>
    /// <remarks>
    /// The payload and nothing else, so both consistency models read one the same way: what a row
    /// says about itself differs between the two stores, but a payload is a serialized model either
    /// way and neither store is involved in opening it.
    /// <para>
    /// Written by <see cref="DomainSerializer"/> and only readable by it — the store's own reads go
    /// through the same one, which is why a payload it wrote round-trips and a hand-edited row may
    /// not.
    /// </para>
    /// </remarks>
    public static OpenedModel Open(Type model, string data)
    {
        try
        {
            var read = DomainSerializer.Current.Deserialize(data, model);

            return read is null
                ? new OpenedModel(null, "The stored payload is empty.")
                : new OpenedModel(read, null);
        }
        catch (Exception exception)
        {
            return new OpenedModel(null, exception.Message);
        }
    }
}

/// <summary>What one stored payload turned out to hold.</summary>
/// <param name="Model">The model, or null when the payload was unreadable.</param>
/// <param name="Error">Why it was unreadable, or null when it was not.</param>
public sealed record OpenedModel(object? Model, string? Error);

/// <summary>The outcome of a load.</summary>
/// <param name="Model">The model, or null when there is no row or its payload was unreadable.</param>
/// <param name="Snapshot">What the row says about itself, or null when there is no row.</param>
/// <param name="Error">Why the row could not be turned into a model, or null when it was.</param>
public sealed record LoadedModel(object? Model, StoredSnapshot? Snapshot, string? Error);

/// <summary>The store's account of one write, as opposed to the state that was written.</summary>
/// <param name="Id">
/// The key the row is held by, whole, as <c>kind:store id:boundary digest</c>.
/// </param>
/// <param name="StoreId">
/// The model's own id inside that key, as <c>id:type version</c> — the same value the list this
/// page is reached from heads its first column with.
/// </param>
/// <param name="ModelType">The binding key the payload was stored under, as <c>name:version</c>.</param>
/// <param name="Version">The version the row was stored at.</param>
/// <param name="LatestPosition">The global position in the log the fold reached.</param>
/// <param name="Created">When it was first stored.</param>
/// <param name="CreatedBy">Who first stored it, or null when the store attributes nothing.</param>
/// <param name="Updated">When it was last stored.</param>
/// <param name="UpdatedBy">Who last stored it, or null when the store attributes nothing.</param>
public sealed record StoredSnapshot(
    string Id,
    string StoreId,
    string ModelType,
    int Version,
    long LatestPosition,
    DateTimeOffset Created,
    string? CreatedBy,
    DateTimeOffset Updated,
    string? UpdatedBy);
