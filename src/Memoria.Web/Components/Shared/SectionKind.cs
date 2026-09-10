namespace Memoria.Web.Components.Shared;

/// <summary>
/// What a tile leads to, as the mark drawn in front of its name.
/// </summary>
/// <remarks>
/// Named rather than left to each page to draw: the same six things are reached from the home page,
/// both overviews and every section page, and a mark that meant one thing on one of them and
/// another elsewhere would be worse than no mark at all. <see cref="SectionMark"/> is where each
/// one is drawn.
/// </remarks>
public enum SectionKind
{
    /// <summary>The types a model declares.</summary>
    Types,

    /// <summary>What has been stored of them.</summary>
    Data,

    /// <summary>The events a model can apply.</summary>
    Events,

    /// <summary>The write models.</summary>
    Aggregates,

    /// <summary>The read models.</summary>
    Projections,

    /// <summary>The streams events are held in, which only the streamed model has.</summary>
    Streams
}
