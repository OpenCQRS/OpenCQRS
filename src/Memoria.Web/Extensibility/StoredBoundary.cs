using System.Text;

namespace Memoria.Web.Extensibility;

/// <summary>
/// A boundary as the store holds it, read back the way a page shows it.
/// </summary>
/// <param name="Tags">The tags it selects on, in the order the store wrote them.</param>
/// <param name="Combination">How they combine, in the words the library uses, or null when there
/// is nothing to tell apart.</param>
/// <remarks>
/// The store writes a boundary as one string, because it has to be one column: the tags of a group
/// joined by an ampersand, the groups joined by a comma. Those two marks are the whole difference
/// between "an event carrying either tag" and "an event carrying both" — which is a difference no
/// reader should be asked to notice in punctuation. So the tags are listed one way whichever it is,
/// and the word beside them is what tells the two apart.
/// </remarks>
public sealed record StoredBoundary(IReadOnlyList<string> Tags, string? Combination)
{
    private const char TagSeparator = '&';

    private const char GroupSeparator = ',';

    private const char EscapeCharacter = '\\';

    /// <summary>
    /// Reads a stored boundary.
    /// </summary>
    /// <param name="boundary">The boundary in canonical form, as the store holds it.</param>
    /// <remarks>
    /// A boundary is built by one of two constructors, and each uses one separator: a union writes
    /// every tag as a group of its own and joins them with commas, an intersection writes them into
    /// one group and joins them with ampersands. So the separator that appears is the answer — and
    /// one carrying both is a shape neither constructor makes, which is named as nothing rather
    /// than guessed at.
    /// </remarks>
    public static StoredBoundary Read(string boundary)
    {
        var tags = new List<string>();
        var tag = new StringBuilder();
        var groups = false;
        var intersections = false;

        for (var index = 0; index < boundary.Length; index++)
        {
            var character = boundary[index];

            // What got a separator past the split is not part of the tag, so it is dropped here and
            // the tag is listed as it was written rather than as it was stored.
            if (character == EscapeCharacter && index + 1 < boundary.Length)
            {
                tag.Append(boundary[++index]);
                continue;
            }

            if (character is GroupSeparator or TagSeparator)
            {
                groups |= character == GroupSeparator;
                intersections |= character == TagSeparator;

                tags.Add(tag.ToString());
                tag.Clear();
                continue;
            }

            tag.Append(character);
        }

        tags.Add(tag.ToString());

        return new StoredBoundary(tags, Combines(groups, intersections));
    }

    /// <summary>
    /// Gets the tags as one line, listed the same way whichever way they combine.
    /// </summary>
    public string Text => string.Join(", ", Tags);

    /// <summary>
    /// The word for the separator that was found, in the library's own vocabulary. One tag has
    /// neither separator and is a union and an intersection at once, so there is nothing there to
    /// tell apart and a marker would only be a word to read past on every such row.
    /// </summary>
    private static string? Combines(bool groups, bool intersections) => (groups, intersections) switch
    {
        (true, false) => "(AnyOf)",
        (false, true) => "(AllOf)",
        _ => null
    };
}
