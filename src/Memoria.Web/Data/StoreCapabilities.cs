namespace Memoria.Web.Data;

/// <summary>
/// What the store the tool was pointed at can be asked for.
/// </summary>
/// <param name="HasDcb">
/// Whether it holds a dynamic consistency boundary: the events, tags and snapshots those pages read.
/// </param>
/// <remarks>
/// A record rather than a provider comparison written wherever it is needed. What a page wants to
/// know is whether there is anything for it to show, and the provider is only how that is worked out
/// today — a second document store, or a relational one installed with the streamed tables alone,
/// would answer the same question differently without any of those pages changing.
/// </remarks>
public sealed record StoreCapabilities(bool HasDcb)
{
    /// <summary>
    /// What a store in this engine can be asked for.
    /// </summary>
    /// <remarks>
    /// Cosmos is the odd one out. The dynamic consistency boundary has one store and it is built on
    /// Entity Framework Core; there is no Cosmos equivalent, and the model would not build on that
    /// provider even if there were. The three relational providers all carry both sets of tables.
    /// </remarks>
    public static StoreCapabilities Of(DatabaseProvider provider) =>
        new(HasDcb: provider is not DatabaseProvider.Cosmos);
}
