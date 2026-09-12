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
    /// Every store can be updated: refreshing a snapshot is a write, and the pages send it through
    /// the framework's own domain service — which the tool registers for a relational store, and now
    /// for a Cosmos one too, carried by the SDK-based domain service over the same client the reads
    /// use. Cosmos remains the odd one out on the dynamic consistency boundary: it has one store,
    /// built on Entity Framework Core, and there is no Cosmos equivalent — the model would not build
    /// on that provider even if there were — so <see cref="HasDcb"/> stays false for it while the
    /// three relational providers carry both.
    /// </remarks>
    public static StoreCapabilities Of(DatabaseProvider provider) =>
        new(HasDcb: provider is not DatabaseProvider.Cosmos,
            CanUpdate: true);
}
