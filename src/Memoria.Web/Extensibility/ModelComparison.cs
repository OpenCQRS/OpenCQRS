using Memoria.EventSourcing;

namespace Memoria.Web.Extensibility;

/// <summary>
/// What the compare tab asks for: which model, folded from which stream through which identifier,
/// at which two versions.
/// </summary>
/// <param name="Model">The aggregate or projection type to fold.</param>
/// <param name="Identity">The stream and identifier worked back out of the row's ids.</param>
/// <param name="StreamId">The stream's id as the store keys it, for the reads that count and place the model's events.</param>
/// <param name="EventTypes">The binding keys of the types the model applies, or null when it applies everything.</param>
/// <param name="From">What the address says for the earlier version, or null when it says nothing.</param>
/// <param name="To">What the address says for the later version, or null when it says nothing.</param>
public sealed record ComparisonRequest(
    Type Model,
    StreamedIdentity Identity,
    string StreamId,
    IReadOnlyList<string>? EventTypes,
    string? From,
    string? To);

/// <summary>
/// Two versions of one model laid over each other: which they are, what each was folded up to, and
/// the rows where they differ.
/// </summary>
/// <param name="Range">The two versions, or null when the address names no pair that can be folded.</param>
/// <param name="Sequences">
/// The sequence each version was folded up to — the model's own event that produced it, or zero
/// for the version before anything happened — or null when nothing was folded.
/// </param>
/// <param name="LastVersion">
/// The model's last version, which is how many of its events there are, or null when the history
/// could not be counted. What a pair is checked against, and the most a form offers.
/// </param>
/// <param name="Rows">The later version's state against the earlier's, or empty when there is nothing to show.</param>
/// <param name="Error">Why there is nothing to show: a pair the address could not be read as, or a fold the store refused.</param>
public sealed record ModelComparison(
    CompareRange? Range,
    (long From, long To)? Sequences,
    long? LastVersion,
    IReadOnlyList<DiffRow> Rows,
    string? Error)
{
    /// <summary>Nothing asked and nothing answered, for a row whose identity could not be rebuilt.</summary>
    public static readonly ModelComparison None = new(null, null, null, [], null);

    /// <summary>
    /// Folds the model at each of the two versions and compares the results.
    /// </summary>
    /// <param name="reads">The tool's own reads, for counting and placing the model's events.</param>
    /// <param name="service">The store's domain service, which does the folding.</param>
    /// <param name="request">Which model, from where, at which versions.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Only once the identifier is known to have been rebuilt: the panel says why when it was not,
    /// and there is nothing to ask the store for. The model's own history is counted first — one
    /// row, newest first, narrowed the way the events tab is — because the count is the last
    /// version, which a pair is checked against and which an address naming no pair compares by
    /// default. A count that failed says nothing rather than nothing found: a history the store
    /// could not read is not an empty one, and the range reader is told the difference.
    /// <para>
    /// A version is then turned into the sequence to fold up to by reading the model's event at
    /// that place in its history, oldest first, one row — the store folds up to a sequence, and a
    /// version's sequence is the one thing it does not know.
    /// </para>
    /// </remarks>
    public static async Task<ModelComparison> Of(
        IStreamedReads reads,
        IDomainService service,
        ComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = request.Identity;

        if (!identity.IsRecovered || identity.Claim is null)
        {
            return None;
        }

        StreamedEventFilter History(int page, bool descending) => new(
            StreamPattern: request.StreamId,
            EventType: null,
            Text: null,
            Descending: descending,
            Page: page,
            Size: 1)
        {
            EventTypes = request.EventTypes,
            Properties = identity.Claim
        };

        var counted = await reads.Events(History(1, descending: true), cancellationToken);

        long? lastVersion = counted.Error is null ? counted.Total : null;

        var reading = CompareRange.Of(request.From, request.To, lastVersion);

        if (reading.Range is not { } range)
        {
            return new ModelComparison(null, null, lastVersion, [], reading.Error);
        }

        if (lastVersion is null)
        {
            return new ModelComparison(range, null, null, [],
                "The history could not be read, so there is no saying which event a version was folded up to.");
        }

        // The sequence a version was folded up to: version zero is the fold up to zero, and every
        // other version is the model's event at that place in its history.
        async Task<long?> SequenceOf(int version)
        {
            if (version == 0)
            {
                return 0;
            }

            var placed = await reads.Events(History(version, descending: false), cancellationToken);

            return placed.Error is null ? placed.Events.FirstOrDefault()?.Event.Position : null;
        }

        var fromSequence = await SequenceOf(range.From);
        var toSequence = await SequenceOf(range.To);

        if (fromSequence is not { } earlier || toSequence is not { } later)
        {
            return new ModelComparison(range, null, lastVersion, [],
                "The event one of the two versions was folded up to could not be found in the history.");
        }

        var before = await ModelFolder.Fold(
            service, request.Model, identity.Stream!, identity.Identifier!, checked((int)earlier), cancellationToken);
        var after = await ModelFolder.Fold(
            service, request.Model, identity.Stream!, identity.Identifier!, checked((int)later), cancellationToken);

        var error = before.Error ?? after.Error ?? (before.Model is null || after.Model is null
            ? "The store folded nothing for one of the two versions."
            : null);

        return error is not null
            ? new ModelComparison(range, (earlier, later), lastVersion, [], error)
            : new ModelComparison(range, (earlier, later), lastVersion, StateDiff.Of(
                DomainTypeDescriber.ReadState(before.Model!),
                DomainTypeDescriber.ReadState(after.Model!)), null);
    }
}
