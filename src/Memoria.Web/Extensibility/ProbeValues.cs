namespace Memoria.Web.Extensibility;

/// <summary>
/// Values distinctive enough to be spotted again in whatever the type built from them produces.
/// </summary>
/// <remarks>
/// Shared by the two things that work out a shape by building one and reading what came back:
/// <see cref="IdentifierShape"/>, which reads the boundary a DCB identifier resolves to, and
/// <see cref="StreamShape"/>, which reads the id a stream produces. One rule for which values can
/// be stood in for, so the two never disagree about which types can be probed at all.
/// </remarks>
internal static class ProbeValues
{
    /// <summary>
    /// A value to pass for one parameter, or null when nothing recognisable can be passed for it.
    /// </summary>
    /// <param name="typeName">The parameter's type, as it would be written in source.</param>
    /// <param name="index">Its place in the constructor, so two of a kind stay apart.</param>
    /// <remarks>
    /// Nothing here may contain <c>%</c> or <c>_</c>: a probe becomes the hole in a LIKE pattern,
    /// and a probe carrying a wildcard of its own would leave one behind in the part that was meant
    /// to stay literal.
    /// </remarks>
    public static string? For(string typeName, int index) => typeName switch
    {
        "string" => $"probe-{index}",
        "Guid" => Guid.NewGuid().ToString(),
        "int" or "long" or "short" => (900_000_000 + index).ToString(),
        _ => null
    };
}
