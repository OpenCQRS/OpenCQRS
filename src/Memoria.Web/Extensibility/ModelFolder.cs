using System.Reflection;
using Memoria.EventSourcing;
using Memoria.EventSourcing.Domain;

namespace Memoria.Web.Extensibility;

/// <summary>
/// Folds one model's state out of its stream up to a sequence, without writing it anywhere.
/// </summary>
/// <remarks>
/// Through the store's own domain service, as <see cref="ModelRefresher"/> refreshes: folding events
/// into a model is the store's operation, and the in-memory read is the one that does it without
/// leaving a snapshot behind — which is what a page comparing two versions of a row wants, since
/// neither version is the one the row should be left at.
/// <para>
/// Reflection, because the model type is not known until someone uploads it and the read is generic
/// and constrained (<c>IAggregateRoot, new()</c>). The overload is picked by its parameters rather
/// than by name: the whole stream, up to a sequence, and up to a date all share the name, and only
/// the sequence one is this.
/// </para>
/// </remarks>
public static class ModelFolder
{
    private static readonly MethodInfo FoldAggregate = UpToSequence(nameof(IDomainService.GetInMemoryAggregate));

    private static readonly MethodInfo FoldProjection = UpToSequence(nameof(IDomainService.GetInMemoryProjection));

    private static MethodInfo UpToSequence(string name) =>
        typeof(IDomainService).GetMethods()
            .SingleOrDefault(method =>
                method.Name == name &&
                method.GetParameters() is [_, _, { ParameterType.Name: nameof(Int32) }, _])
        ?? throw new InvalidOperationException($"IDomainService.{name} up to a sequence is missing.");

    /// <summary>
    /// Folds a streamed model's state up to a sequence.
    /// </summary>
    /// <param name="service">The streamed domain service.</param>
    /// <param name="model">The aggregate or projection type to fold.</param>
    /// <param name="streamId">The stream it is folded from.</param>
    /// <param name="identifier">An identifier instance naming it inside that stream.</param>
    /// <param name="upToSequence">The last sequence to fold, inclusive.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// The folded model, or what went wrong. A stream with nothing to fold up to that sequence
    /// comes back as the model in its opening state, which is an answer rather than a fault.
    /// </returns>
    /// <remarks>
    /// Which of the store's two reads is called follows from the identifier rather than being
    /// asked for, as with a refresh: an identifier names one model and only one kind of model.
    /// </remarks>
    public static async Task<FoldedModel> Fold(
        IDomainService service,
        Type model,
        object streamId,
        object identifier,
        int upToSequence,
        CancellationToken cancellationToken = default)
    {
        if (streamId is not IStreamId)
        {
            return new FoldedModel(null,
                $"{streamId.GetType().Name} is not a stream, so there is nowhere to fold from.");
        }

        var read = identifier switch
        {
            IAggregateId => FoldAggregate,
            IProjectionId => FoldProjection,
            _ => null
        };

        if (read is null)
        {
            return new FoldedModel(null,
                $"{identifier.GetType().Name} is not a streamed identifier, so nothing names what to fold.");
        }

        var answer = await StoreCall.Invoke(
            read, model, service, [streamId, identifier, upToSequence, cancellationToken]);

        return new FoldedModel(answer.Value, answer.Error);
    }
}

/// <summary>The outcome of a fold.</summary>
/// <param name="Model">The folded model, or null when there is none to show.</param>
/// <param name="Error">Why there is none, or null when nothing went wrong.</param>
public sealed record FoldedModel(object? Model, string? Error);
