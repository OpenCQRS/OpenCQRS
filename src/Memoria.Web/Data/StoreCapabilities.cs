namespace Memoria.Web.Data;

/// <summary>
/// What the store the tool was pointed at can be asked for.
/// </summary>
/// <param name="HasDcb">
/// Whether it holds a dynamic consistency boundary: the events, tags and snapshots those pages read.
/// </param>
/// <param name="CanUpdate">
/// Whether a stored snapshot can be brought up to date in it, which is the one write the model
/// pages offer.
/// </param>
/// <remarks>
/// A record rather than a provider comparison written wherever it is needed. What a page wants to
/// know is whether there is anything for it to show, and the provider is only how that is worked out
/// today — a second document store, or a relational one installed with the streamed tables alone,
/// would answer the same question differently without any of those pages changing.
/// </remarks>
public sealed record StoreCapabilities(bool HasDcb, bool CanUpdate)
{
    /// <summary>
    /// What a store in this engine can be asked for.
    /// </summary>
    /// <remarks>
    /// Cosmos is the odd one out on both counts. The dynamic consistency boundary has one store and
    /// it is built on Entity Framework Core; there is no Cosmos equivalent, and the model would not
    /// build on that provider even if there were. Refreshing a snapshot is a write, and the pages
    /// send it through the framework's own domain service — which the tool registers for a
    /// relational store and not for a Cosmos one, where the documents are read through the SDK
    /// instead. The three relational providers carry both.
    /// </remarks>
    public static StoreCapabilities Of(DatabaseProvider provider) =>
        new(HasDcb: provider is not DatabaseProvider.Cosmos,
            CanUpdate: provider is not DatabaseProvider.Cosmos);
}
