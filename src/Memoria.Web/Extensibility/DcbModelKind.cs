using Memoria.EventSourcing.Dcb;
using Memoria.EventSourcing.Dcb.Store.EntityFrameworkCore.Entities;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Which of the two DCB models a page is reading.
/// </summary>
/// <remarks>
/// The store keeps both in one table "because they differ only in which identifier names them", and
/// the pages that read it are the same shape for the same reason. What differs is named here once —
/// the discriminator on the row, the attribute the binding key is read off, and which list of the
/// catalogue the types come from — rather than in each page that has to know it.
/// </remarks>
public enum DcbModelKind
{
    /// <summary>A write model: folded from a boundary, and able to produce events.</summary>
    Aggregate,

    /// <summary>A read model: folded from a boundary, and never producing any.</summary>
    Projection
}

/// <summary>
/// What each kind of model is called by the store, the catalogue and the framework.
/// </summary>
public static class DcbModels
{
    /// <summary>
    /// The discriminator the snapshot row carries, which is what tells one kind's rows from the
    /// other's in the single table holding both.
    /// </summary>
    public static string SnapshotKind(this DcbModelKind kind) => kind switch
    {
        DcbModelKind.Projection => DcbSnapshotEntity.ProjectionKind,
        _ => DcbSnapshotEntity.AggregateKind
    };

    /// <summary>
    /// The key the store writes a model's snapshots under, as <c>name:version</c>.
    /// </summary>
    /// <param name="kind">Which model it is.</param>
    /// <param name="model">The model type, carrying the attribute the key is read off.</param>
    /// <exception cref="InvalidOperationException">The type carries no attribute.</exception>
    public static string BindingKey(this DcbModelKind kind, Type model) => kind switch
    {
        DcbModelKind.Projection => DcbTypeBindings.GetProjectionBindingKey(model),
        _ => DcbTypeBindings.GetAggregateBindingKey(model)
    };

    /// <summary>The models of one kind that the uploaded assemblies declare.</summary>
    public static IReadOnlyList<Type> Models(this DomainTypeCatalogue catalogue, DcbModelKind kind) =>
        kind switch
        {
            DcbModelKind.Projection => catalogue.DcbProjections,
            _ => catalogue.DcbAggregates
        };

    /// <summary>The identifiers that could address a model of one kind.</summary>
    public static IReadOnlyList<Type> Identifiers(this DomainTypeCatalogue catalogue, DcbModelKind kind) =>
        kind switch
        {
            DcbModelKind.Projection => catalogue.DcbProjectionIds,
            _ => catalogue.DcbAggregateIds
        };

    /// <summary>
    /// The boundary an identifier carries, whichever kind of model it names.
    /// </summary>
    /// <param name="identifier">An identifier instance, as the factory built it.</param>
    /// <returns>The boundary, or null when the instance is not a DCB identifier at all.</returns>
    public static TagQuery? BoundaryOf(object? identifier) => identifier switch
    {
        IDcbAggregateId aggregateId => aggregateId.Boundary,
        IDcbProjectionId projectionId => projectionId.Boundary,
        _ => null
    };
}
