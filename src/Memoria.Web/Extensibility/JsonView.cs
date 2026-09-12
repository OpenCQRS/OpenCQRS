using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Lays a stored payload out to be read: one value per line, nested values stepped in under the
/// name that holds them, and each kind of token wrapped so the stylesheet can colour it.
/// </summary>
/// <remarks>
/// The store holds a payload as one line, which is right for a column and wrong for a reader. This
/// re-lays that line rather than re-serialising the model it opened into: what the tab shows is the
/// row's own text, so a payload the model cannot read back is still shown, and a value the
/// serializer wrote one way is not shown another.
/// <para>
/// Rendered on the server as text rather than by script in the browser, because these pages render
/// statically and nothing else on them runs script to draw itself. The markup is built here and
/// nowhere else, so the one thing that has to hold — nothing in a payload reaches the page as
/// markup — is held in one place.
/// </para>
/// </remarks>
public static class JsonView
{
    private const string Indent = "  ";

    /// <summary>
    /// Renders one payload.
    /// </summary>
    /// <param name="data">The payload, as the store wrote it.</param>
    /// <returns>The markup to write inside a <c>&lt;pre&gt;</c>, or why there is none.</returns>
    public static RenderedJson Render(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return new RenderedJson(null, "The stored payload is empty.");
        }

        try
        {
            using var document = JsonDocument.Parse(data);

            var markup = new StringBuilder();

            Write(markup, document.RootElement, depth: 0);

            return new RenderedJson(markup.ToString(), null);
        }
        catch (JsonException exception)
        {
            return new RenderedJson(null, exception.Message);
        }
    }

    private static void Write(StringBuilder markup, JsonElement element, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(markup, element, depth);
                break;

            case JsonValueKind.Array:
                WriteArray(markup, element, depth);
                break;

            case JsonValueKind.String:
                WriteToken(markup, "json-string", element.GetRawText());
                break;

            case JsonValueKind.Number:
                WriteToken(markup, "json-number", element.GetRawText());
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                WriteToken(markup, "json-bool", element.GetRawText());
                break;

            default:
                WriteToken(markup, "json-null", "null");
                break;
        }
    }

    private static void WriteObject(StringBuilder markup, JsonElement element, int depth)
    {
        var first = true;

        foreach (var property in element.EnumerateObject())
        {
            markup.Append(first ? "{\n" : ",\n");
            first = false;

            Pad(markup, depth + 1);
            // Re-quoted the way the store quotes a name. The relaxed encoder keeps characters like
            // < and & as themselves in the JSON, and the HTML escaping below is what keeps them
            // out of the markup — one escaping per layer rather than two piled up.
            WriteToken(markup, "json-key",
                $"\"{JsonEncodedText.Encode(property.Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)}\"");
            markup.Append(": ");
            Write(markup, property.Value, depth + 1);
        }

        if (first)
        {
            markup.Append("{}");
            return;
        }

        markup.Append('\n');
        Pad(markup, depth);
        markup.Append('}');
    }

    private static void WriteArray(StringBuilder markup, JsonElement element, int depth)
    {
        var first = true;

        foreach (var item in element.EnumerateArray())
        {
            markup.Append(first ? "[\n" : ",\n");
            first = false;

            Pad(markup, depth + 1);
            Write(markup, item, depth + 1);
        }

        if (first)
        {
            markup.Append("[]");
            return;
        }

        markup.Append('\n');
        Pad(markup, depth);
        markup.Append(']');
    }

    /// <summary>
    /// One token, escaped so a payload holding markup is shown rather than rendered.
    /// </summary>
    /// <remarks>
    /// Only the three characters that mean something in a text node. The general HTML encoder also
    /// turns every quote into an entity, and a JSON payload is mostly quotes — the source would be
    /// unreadable and the page would be several times the size for no safety it did not already
    /// have.
    /// </remarks>
    private static void WriteToken(StringBuilder markup, string cssClass, string text) =>
        markup.Append("<span class=\"").Append(cssClass).Append("\">")
            .Append(text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"))
            .Append("</span>");

    private static void Pad(StringBuilder markup, int depth)
    {
        for (var step = 0; step < depth; step++)
        {
            markup.Append(Indent);
        }
    }
}

/// <summary>What one payload rendered to.</summary>
/// <param name="Markup">
/// The HTML to write inside a <c>&lt;pre&gt;</c>, or null when the payload could not be laid out.
/// </param>
/// <param name="Error">Why it could not be, or null when it was.</param>
public sealed record RenderedJson(string? Markup, string? Error);
