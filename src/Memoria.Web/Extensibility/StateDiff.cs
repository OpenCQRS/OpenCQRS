namespace Memoria.Web.Extensibility;

/// <summary>
/// What changed between two readings of a model's state, as one table with both values on every
/// row and a mark on the rows that differ.
/// </summary>
/// <remarks>
/// Rows are matched by name at each level, because that is what the state reader keys them by — a
/// property by its name and a list element by its position — and names are compared exactly, as the
/// language compares them. The rows come out in the later reading's order, since that is the shape
/// the model has now, with a row the later reading no longer has slotted where the earlier one had
/// it, so a reader running down the table meets it among its old neighbours.
/// <para>
/// A change inside a shape is a change to the shape: a parent whose children differ, or whose own
/// value differs, is marked changed, so a reader scanning the top level is told where to look. A
/// row on one side only carries its insides through marked the same way, so an added or removed
/// shape still says what it holds.
/// </para>
/// </remarks>
public static class StateDiff
{
    /// <summary>
    /// Compares two readings of a model's state.
    /// </summary>
    /// <param name="from">The earlier reading.</param>
    /// <param name="to">The later reading.</param>
    public static IReadOnlyList<DiffRow> Of(
        IReadOnlyList<DomainPropertyValue> from, IReadOnlyList<DomainPropertyValue> to)
    {
        var earlier = from.ToDictionary(row => row.Name, StringComparer.Ordinal);
        var later = to.Select(row => row.Name).ToHashSet(StringComparer.Ordinal);

        // The earlier rows that no longer exist, keyed by the earlier row they came after among
        // those that still do — or by nothing, for the ones that came first.
        var removedAfter = new Dictionary<string, List<DomainPropertyValue>>(StringComparer.Ordinal);
        var removedFirst = new List<DomainPropertyValue>();
        string? lastKept = null;

        foreach (var row in from)
        {
            if (later.Contains(row.Name))
            {
                lastKept = row.Name;
            }
            else if (lastKept is null)
            {
                removedFirst.Add(row);
            }
            else
            {
                (removedAfter.TryGetValue(lastKept, out var bucket)
                    ? bucket
                    : removedAfter[lastKept] = []).Add(row);
            }
        }

        var rows = new List<DiffRow>(removedFirst.Select(Removed));

        foreach (var row in to)
        {
            rows.Add(earlier.TryGetValue(row.Name, out var before) ? Compare(before, row) : Added(row));

            if (removedAfter.TryGetValue(row.Name, out var gone))
            {
                rows.AddRange(gone.Select(Removed));
            }
        }

        return rows;
    }

    private static DiffRow Compare(DomainPropertyValue before, DomainPropertyValue after)
    {
        var children = Of(before.Children, after.Children);

        var change =
            before.Value != after.Value || children.Any(child => child.Change != Change.Unchanged)
                ? Change.Changed
                : Change.Unchanged;

        return new DiffRow(after.Name, after.TypeName, before.Value, after.Value, change, children);
    }

    private static DiffRow Added(DomainPropertyValue row) =>
        new(row.Name, row.TypeName, null, row.Value, Change.Added, row.Children.Select(Added).ToList());

    private static DiffRow Removed(DomainPropertyValue row) =>
        new(row.Name, row.TypeName, row.Value, null, Change.Removed, row.Children.Select(Removed).ToList());
}

/// <summary>How one row differs between two readings.</summary>
public enum Change
{
    /// <summary>Present in both, and the same in both, insides included.</summary>
    Unchanged,

    /// <summary>Present in both, and different: the value, or something inside it.</summary>
    Changed,

    /// <summary>Present in the later reading only.</summary>
    Added,

    /// <summary>Present in the earlier reading only.</summary>
    Removed
}

/// <summary>
/// One row of a comparison: a property, or an element of a list, with what it held in each reading.
/// </summary>
/// <param name="Name">The property's name, or a list element's position.</param>
/// <param name="TypeName">
/// The type it is declared as in the later reading, or in the earlier one when the later has no
/// such row.
/// </param>
/// <param name="From">What the earlier reading held, or null when it had no such row or the value was unset.</param>
/// <param name="To">What the later reading held, or null when it has no such row or the value is unset.</param>
/// <param name="Change">How the two differ.</param>
/// <param name="Children">What is inside the value, compared the same way.</param>
public sealed record DiffRow(
    string Name,
    string TypeName,
    string? From,
    string? To,
    Change Change,
    IReadOnlyList<DiffRow> Children);
