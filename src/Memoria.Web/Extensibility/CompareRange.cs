using System.Globalization;

namespace Memoria.Web.Extensibility;

/// <summary>
/// The two sequences the compare tab folds up to: the earlier fold, and the later one it is
/// compared against.
/// </summary>
/// <param name="From">The last sequence the earlier fold applies, inclusive. Zero is the state before anything happened.</param>
/// <param name="To">The last sequence the later fold applies, inclusive.</param>
public sealed record CompareRange(int From, int To)
{
    /// <summary>
    /// Reads a pair out of what the address carries.
    /// </summary>
    /// <param name="from">What the address says for the earlier sequence, or null when it says nothing.</param>
    /// <param name="to">What the address says for the later sequence, or null when it says nothing.</param>
    /// <param name="lastSequence">
    /// The last sequence there is to fold up to, or null when the history could not be counted.
    /// </param>
    /// <returns>The pair, or why what the address carries is not one.</returns>
    /// <remarks>
    /// An address naming neither compares the last event against the state before it — the pair a
    /// reader arriving at the tab with nothing chosen most plausibly wants — which needs a last
    /// event to exist and to be known. An address naming one and not the other is refused rather
    /// than half-guessed. Each is read as an <c>int</c> because that is what the store folds up
    /// to; a sequence past that is not one the store could reach, and is refused the same way as
    /// a word.
    /// </remarks>
    public static RangeReading Of(string? from, string? to, long? lastSequence)
    {
        var fromMissing = string.IsNullOrWhiteSpace(from);
        var toMissing = string.IsNullOrWhiteSpace(to);

        if (fromMissing && toMissing)
        {
            return lastSequence switch
            {
                null => new RangeReading(null,
                    "The history could not be counted, so there is no last event to compare by default."),
                < 1 => new RangeReading(null,
                    "The stream holds no events this aggregate applies, so there is nothing to compare."),
                _ => new RangeReading(new CompareRange((int)(lastSequence.Value - 1), (int)lastSequence.Value), null)
            };
        }

        if (fromMissing || toMissing)
        {
            return new RangeReading(null,
                "Both sequences are needed to compare: the one to fold up to first, and the one to carry on to.");
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
                $"The first sequence, {earlier}, must be below the second, {later}: a fold is compared against one carried further on.");
        }

        if (lastSequence is { } last && later > last)
        {
            return new RangeReading(null,
                $"There is nothing past sequence {last} to fold up to, so {later} is out of reach.");
        }

        return new RangeReading(new CompareRange(earlier, later), null);
    }
}

/// <summary>What the address was read as.</summary>
/// <param name="Range">The pair, or null when the address does not name one.</param>
/// <param name="Error">Why it does not, or null when it does.</param>
public sealed record RangeReading(CompareRange? Range, string? Error);
