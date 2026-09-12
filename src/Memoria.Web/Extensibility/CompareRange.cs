using System.Globalization;

namespace Memoria.Web.Extensibility;

/// <summary>
/// The two versions the compare tab folds: the earlier one, and the later one it is compared
/// against.
/// </summary>
/// <remarks>
/// Versions rather than sequences, because a model's versions count its own events from one and
/// climb by one on every row of its history, whereas a sequence counts the stream — on a stream
/// several models share, every other model's events too. Version zero is the model before anything
/// happened to it.
/// </remarks>
/// <param name="From">The earlier version.</param>
/// <param name="To">The later version.</param>
public sealed record CompareRange(int From, int To)
{
    /// <summary>
    /// The same pair one version earlier, or null when it is already against version zero.
    /// </summary>
    /// <remarks>
    /// The same distance apart: stepping is moving up and down the events table, and a pair a row
    /// wide stays a row wide as it goes.
    /// </remarks>
    public CompareRange? Earlier() => From > 0 ? new CompareRange(From - 1, To - 1) : null;

    /// <summary>
    /// The same pair one version later, or null when the later version would be past the last —
    /// or when the last is not known, since a step into the unknown would be a link to an error.
    /// </summary>
    /// <param name="lastVersion">The model's last version, or null when the history could not be counted.</param>
    public CompareRange? Later(long? lastVersion) =>
        lastVersion is { } last && To < last ? new CompareRange(From + 1, To + 1) : null;

    /// <summary>
    /// Reads a pair out of what the address carries.
    /// </summary>
    /// <param name="from">What the address says for the earlier version, or null when it says nothing.</param>
    /// <param name="to">What the address says for the later version, or null when it says nothing.</param>
    /// <param name="lastVersion">
    /// The model's last version — how many of its events there are — or null when the history
    /// could not be counted.
    /// </param>
    /// <returns>The pair, or why what the address carries is not one.</returns>
    /// <remarks>
    /// An address naming neither compares the last version against the one before it — the pair a
    /// reader arriving at the tab with nothing chosen most plausibly wants — which needs a last
    /// version to exist and to be known. An address naming one and not the other is refused rather
    /// than half-guessed. Each is read as an <c>int</c>, which is what a version is stored as; a
    /// number past that is refused the same way as a word.
    /// </remarks>
    public static RangeReading Of(string? from, string? to, long? lastVersion)
    {
        var fromMissing = string.IsNullOrWhiteSpace(from);
        var toMissing = string.IsNullOrWhiteSpace(to);

        if (fromMissing && toMissing)
        {
            return lastVersion switch
            {
                null => new RangeReading(null,
                    "The history could not be counted, so there is no last version to compare by default."),
                < 1 => new RangeReading(null,
                    "The stream holds no events this model applies, so there is nothing to compare."),
                _ => new RangeReading(new CompareRange((int)(lastVersion.Value - 1), (int)lastVersion.Value), null)
            };
        }

        if (fromMissing || toMissing)
        {
            return new RangeReading(null,
                "Both versions are needed to compare: the earlier one, and the later one to hold it against.");
        }

        if (!int.TryParse(from, NumberStyles.None, CultureInfo.InvariantCulture, out var earlier) ||
            !int.TryParse(to, NumberStyles.None, CultureInfo.InvariantCulture, out var later))
        {
            return new RangeReading(null,
                $"'{from}' and '{to}' must both be whole numbers from zero up to {int.MaxValue}.");
        }

        if (earlier >= later)
        {
            return new RangeReading(null,
                $"The first version, {earlier}, must be below the second, {later}: a version is compared against a later one.");
        }

        if (lastVersion is { } last && later > last)
        {
            return new RangeReading(null,
                $"The model has no version past {last}, so {later} is out of reach.");
        }

        return new RangeReading(new CompareRange(earlier, later), null);
    }
}

/// <summary>What the address was read as.</summary>
/// <param name="Range">The pair, or null when the address does not name one.</param>
/// <param name="Error">Why it does not, or null when it does.</param>
public sealed record RangeReading(CompareRange? Range, string? Error);
