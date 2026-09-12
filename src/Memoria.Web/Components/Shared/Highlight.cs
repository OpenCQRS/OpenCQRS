namespace Memoria.Web.Components.Shared;

/// <summary>
/// One stretch of a value, either the text a filter was typed against or the text between two
/// such stretches.
/// </summary>
/// <param name="Text">The stretch, as the value has it.</param>
/// <param name="IsMatch">Whether this is what was typed.</param>
public sealed record TextRun(string Text, bool IsMatch);

/// <summary>
/// Cuts a value into the stretches that match what was typed into a filter and the stretches
/// between them, so a filtered table can mark the text each row was kept for.
/// </summary>
/// <remarks>
/// Matched the way the stores match it — trimmed, anywhere in the value, and without regard to
/// case — so a mark falls exactly where the store's match did and nowhere else. Kept apart from
/// the component that draws the runs, because where the cuts fall is the whole of what there is to
/// pin down, and it is pinned without rendering anything.
/// </remarks>
public static class Highlight
{
    /// <summary>
    /// Cuts one value.
    /// </summary>
    /// <param name="text">The value a row carries.</param>
    /// <param name="needle">What was typed, or null or blank when nothing was.</param>
    /// <returns>
    /// The value in order, as runs that alternate between matched and not. Empty for an empty
    /// value; one unmatched run when nothing was typed or nothing matched.
    /// </returns>
    public static IReadOnlyList<TextRun> Runs(string text, string? needle)
    {
        if (text.Length == 0)
        {
            return [];
        }

        var wanted = needle?.Trim();

        if (string.IsNullOrEmpty(wanted))
        {
            return [new TextRun(text, IsMatch: false)];
        }

        var runs = new List<TextRun>();
        var from = 0;

        while (from < text.Length)
        {
            var at = text.IndexOf(wanted, from, StringComparison.OrdinalIgnoreCase);

            if (at < 0)
            {
                runs.Add(new TextRun(text[from..], IsMatch: false));
                break;
            }

            if (at > from)
            {
                runs.Add(new TextRun(text[from..at], IsMatch: false));
            }

            // As long as the value has it rather than as long as what was typed: a match that
            // ignores case can differ in length from the needle under some cultures, and the mark
            // has to cover exactly what was found.
            var matched = text[at..(at + wanted.Length)];
            runs.Add(new TextRun(matched, IsMatch: true));
            from = at + wanted.Length;
        }

        return runs;
    }
}
