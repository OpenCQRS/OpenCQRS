using Memoria.EventSourcing;

namespace Memoria.Web.Extensibility;

/// <summary>
/// What the compare tab asks for: which model, folded from which stream through which identifier,
/// up to which two sequences.
/// </summary>
/// <param name="Model">The aggregate or projection type to fold.</param>
/// <param name="Identity">The stream and identifier worked back out of the row's ids.</param>
/// <param name="StreamId">The stream's id as the store keys it, for the read that finds the last event.</param>
/// <param name="EventTypes">The binding keys of the types the model applies, or null when it applies everything.</param>
/// <param name="From">What the address says for the earlier sequence, or null when it says nothing.</param>
/// <param name="To">What the address says for the later sequence, or null when it says nothing.</param>
public sealed record ComparisonRequest(
    Type Model,
    StreamedIdentity Identity,
    string StreamId,
    IReadOnlyList<string>? EventTypes,
    string? From,
    string? To);

/// <summary>
/// Two folds of one model laid over each other: which sequences, and the rows where they differ.
/// </summary>
/// <param name="Range">The two sequences folded up to, or null when the address names no pair that can be.</param>
/// <param name="LastSequence">
/// The last sequence the model has to fold up to, or null when the history could not be counted.
/// What a pair is checked against, and the most a form offers.
/// </param>
/// <param name="Rows">The later fold's state against the earlier's, or empty when there is nothing to show.</param>
/// <param name="Error">Why there is nothing to show: a pair the address could not be read as, or a fold the store refused.</param>
public sealed record ModelComparison(
    CompareRange? Range,
    long? LastSequence,
    IReadOnlyList<DiffRow> Rows,
    string? Error)
{
    /// <summary>Nothing asked and nothing answered, for a row whose identity could not be rebuilt.</summary>
    public static readonly ModelComparison None = new(null, null, [], null);

    /// <summary>
    /// Folds the model up to each of the two sequences and compares the results.
    /// </summary>
    /// <param name="reads">The tool's own reads, for the model's last event.</param>
    /// <param name="service">The store's domain service, which does the folding.</param>
    /// <param name="request">Which model, from where, up to which sequences.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Only once the identifier is known to have been rebuilt: the panel says why when it was not,
    /// and there is nothing to ask the store for. The model's own last event is read first — one
    /// row, newest first, narrowed the way the events tab is — because it is what a pair is checked
    /// against and what an address naming no pair compares by default. A count that failed says
    /// nothing rather than nothing found: a history the store could not read is not an empty one,
    /// and the range reader is told the difference.
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

        var history = await reads.Events(new StreamedEventFilter(
            StreamPattern: request.StreamId,
            EventType: null,
            Text: null,
            Descending: true,
            Page: 1,
            Size: 1)
        {
            EventTypes = request.EventTypes,
            Properties = identity.Claim
        }, cancellationToken);

        long? lastSequence = history.Error is null
            ? history.Events.FirstOrDefault()?.Event.Position ?? 0
            : null;

        var reading = CompareRange.Of(request.From, request.To, lastSequence);

        if (reading.Range is not { } range)
        {
            return new ModelComparison(null, lastSequence, [], reading.Error);
        }

        var before = await ModelFolder.Fold(
            service, request.Model, identity.Stream!, identity.Identifier!, range.From, cancellationToken);
        var after = await ModelFolder.Fold(
            service, request.Model, identity.Stream!, identity.Identifier!, range.To, cancellationToken);

        var error = before.Error ?? after.Error ?? (before.Model is null || after.Model is null
            ? "The store folded nothing for one of the two sequences."
            : null);

        return error is not null
            ? new ModelComparison(range, lastSequence, [], error)
            : new ModelComparison(range, lastSequence, StateDiff.Of(
                DomainTypeDescriber.ReadState(before.Model!),
                DomainTypeDescriber.ReadState(after.Model!)), null);
    }
}
